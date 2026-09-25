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
                bool intact = await _auditLog.VerifyChainAsync().ConfigureAwait(false);
                if (intact)
                {
                    return HealthCheckResult.Healthy("DPP audit chain verified.");
                }

                // Deliberately Unhealthy rather than Degraded: a chain that does not verify means the
                // non-repudiation guarantee the log exists to provide is already void, and the
                // specific entry at fault is reported by DppAuditLog itself.
                _logger.LogCritical("DPP audit chain verification FAILED: entries were altered, inserted or removed.");
                return HealthCheckResult.Unhealthy("DPP audit chain verification failed; the log has been altered or truncated.");
            }
            catch (DppAuditException ex)
            {
                // The log could not be read at all, so its integrity is unknown. That is not the same
                // as proven-tampered, but it is equally not a clean bill of health.
                _logger.LogError(ex, "DPP audit chain could not be verified.");
                return HealthCheckResult.Unhealthy("DPP audit chain could not be read for verification.", ex);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "DPP audit chain could not be verified.");
                return HealthCheckResult.Unhealthy("DPP audit chain could not be read for verification.", ex);
            }
        }
    }
}
