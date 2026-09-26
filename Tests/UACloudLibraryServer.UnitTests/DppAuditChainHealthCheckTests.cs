using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Tests that the audit health check reports each verification outcome as what it actually is.
    /// </summary>
    /// <remarks>
    /// All three failure shapes are <see cref="HealthStatus.Unhealthy"/>, so the status alone does
    /// not distinguish them - the description is the only thing an operator reads. Announcing a
    /// documented migration state as "the log has been altered or truncated" is the same false-alarm
    /// failure the unverifiable-entry path already avoids, and a tamper alert that cries wolf gets
    /// muted, costing the real signal.
    /// </remarks>
    public class DppAuditChainHealthCheckTests
    {
        private sealed class StubAuditLog : IDppAuditLog
        {
            private readonly DppAuditVerificationOutcome? _outcome;
            private readonly System.Exception _throw;

            public StubAuditLog(DppAuditVerificationOutcome outcome) => _outcome = outcome;

            public StubAuditLog(System.Exception toThrow) => _throw = toThrow;

            public Task RecordAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome, string operationId = null) =>
                Task.CompletedTask;

            public Task<DppAuditVerificationOutcome> VerifyChainAsync(CancellationToken cancellationToken = default) =>
                _throw is not null ? Task.FromException<DppAuditVerificationOutcome>(_throw) : Task.FromResult(_outcome.Value);
        }

        private static Task<HealthCheckResult> RunAsync(IDppAuditLog log) =>
            new DppAuditChainHealthCheck(log, NullLoggerFactory.Instance)
                .CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        [Fact]
        public async Task VerifiedChain_IsHealthy()
        {
            HealthCheckResult result = await RunAsync(new StubAuditLog(DppAuditVerificationOutcome.Verified));

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task TamperedChain_IsReportedAsAlteration()
        {
            HealthCheckResult result = await RunAsync(new StubAuditLog(DppAuditVerificationOutcome.Tampered));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("altered or truncated", result.Description, System.StringComparison.Ordinal);
        }

        [Fact]
        public async Task MigrationRequired_IsUnhealthy_ButNotReportedAsTampering()
        {
            // The reported issue: this state also fails verification, but it is just as likely an
            // un-migrated log as an attack, so the description must not assert alteration.
            HealthCheckResult result = await RunAsync(new StubAuditLog(DppAuditVerificationOutcome.MigrationRequired));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.DoesNotContain("altered or truncated", result.Description, System.StringComparison.Ordinal);
            Assert.DoesNotContain("verification failed", result.Description, System.StringComparison.OrdinalIgnoreCase);

            // It must still point the operator at the resolution for the legitimate case.
            Assert.Contains("migration", result.Description, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UnverifiableChain_IsReportedAsUnknown_NotTampered()
        {
            // An entry signed with a retired key that is no longer configured: integrity is unknown
            // rather than disproven, which must not read as an intrusion either.
            HealthCheckResult result = await RunAsync(new StubAuditLog(new DppAuditException("key 'ABC' is not configured")));

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("could not be verified", result.Description, System.StringComparison.Ordinal);
            Assert.DoesNotContain("altered or truncated", result.Description, System.StringComparison.Ordinal);
        }

        [Fact]
        public async Task ProbeCancellation_PropagatesRatherThanReportingUnhealthy()
        {
            // A probe that gave up says nothing about the chain; turning that into an Unhealthy
            // result would be a tampering-shaped alert caused by a timeout.
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var check = new DppAuditChainHealthCheck(
                new StubAuditLog(new System.OperationCanceledException(cts.Token)),
                NullLoggerFactory.Instance);

            await Assert.ThrowsAnyAsync<System.OperationCanceledException>(
                () => check.CheckHealthAsync(new HealthCheckContext(), cts.Token));
        }
    }
}
