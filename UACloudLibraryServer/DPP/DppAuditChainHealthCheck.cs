using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Health check that re-walks the DPP audit chain and compares it against the persisted
    /// checkpoint.
    /// </summary>
    /// <remarks>
    /// Tamper evidence only has operational value if something actually looks. Hash-chaining and the
    /// keyed checkpoint make alteration and truncation <em>detectable</em>, but nothing raises a
    /// signal unless the chain is verified, so a tampered log would otherwise sit unnoticed until
    /// somebody manually asked. Exposing verification as a health check puts it on the same footing
    /// as any other dependency failure: it is polled by whatever already monitors the service, and an
    /// <see cref="HealthStatus.Unhealthy"/> result is an alert rather than a log line nobody reads.
    /// <para>
    /// Registered as a non-default (tagged) check so it is not run on every liveness probe: it reads
    /// the whole audit table, so it belongs on a slower readiness/monitoring schedule.
    /// </para>
    /// </remarks>
    public sealed class DppAuditChainHealthCheck : IHealthCheck
    {
        /// <summary>Tag identifying this check, so probes can opt in or out of running it.</summary>
        public const string Tag = "dpp-audit";

        /// <summary>Registered name, also used as the health-report entry key.</summary>
        public const string Name = "dpp-audit-chain";

        private readonly IDppAuditLog _auditLog;
        private readonly ILogger _logger;

        public DppAuditChainHealthCheck(IDppAuditLog auditLog, ILoggerFactory loggerFactory)
        {
            _auditLog = auditLog;
            _logger = loggerFactory.CreateLogger("DppAuditChainHealthCheck");
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Forward the probe's token: this reads the entire audit table, so a timed-out or
                // disconnected probe must actually stop the scan rather than leaving it running.
                // Otherwise repeated probes stack up abandoned full-table reads as the log grows.
                DppAuditVerificationOutcome outcome = await _auditLog.VerifyChainAsync(cancellationToken).ConfigureAwait(false);

                switch (outcome)
                {
                    case DppAuditVerificationOutcome.Verified:
                        return HealthCheckResult.Healthy("DPP audit chain verified.");

                    case DppAuditVerificationOutcome.MigrationRequired:
                        // Unhealthy, because the chain is not authenticated and appends are refused -
                        // but described as a migration rather than an intrusion. The data cannot tell
                        // this apart from a downgrade, so the message names both possibilities and
                        // points at the setting that resolves the legitimate one. Asserting tampering
                        // here would produce the same false alarm the unverifiable-entry path below
                        // is careful to avoid.
                        _logger.LogCritical(
                            "DPP audit chain is not authenticated: the checkpoint predates the configured audit key. Complete the documented migration, or treat this as a downgrade if the log was expected to be keyed already.");
                        return HealthCheckResult.Unhealthy(
                            "DPP audit chain is not authenticated: the checkpoint is unkeyed while keyed auditing is enabled. " +
                            "This is either an un-migrated log, which the checkpoint-migration setting resolves, or a downgrade to bypass verification.");

                    case DppAuditVerificationOutcome.Tampered:
                    default:
                        // Deliberately Unhealthy rather than Degraded: a chain that does not verify
                        // means the non-repudiation guarantee the log exists to provide is already
                        // void, and the specific entry at fault is reported by DppAuditLog itself.
                        _logger.LogCritical("DPP audit chain verification FAILED: entries were altered, inserted or removed.");
                        return HealthCheckResult.Unhealthy("DPP audit chain verification failed; the log has been altered or truncated.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The probe gave up, which says nothing about the chain. Reporting Unhealthy here
                // would turn a probe timeout into a tampering-shaped alert.
                throw;
            }
            catch (DppAuditException ex)
            {
                // Integrity is *unknown*, not disproven: the log could not be read, or an entry was
                // signed with a key that is no longer configured. Keeping this distinct from the
                // failure above matters - reporting "unverifiable" as "tampered" would produce false
                // alarms on an ordinary key rotation, and an alert that cries wolf gets muted.
                _logger.LogError(ex, "DPP audit chain could not be verified.");
                return HealthCheckResult.Unhealthy($"DPP audit chain could not be verified: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "DPP audit chain could not be verified.");
                return HealthCheckResult.Unhealthy("DPP audit chain could not be read for verification.", ex);
            }
        }
    }
}
