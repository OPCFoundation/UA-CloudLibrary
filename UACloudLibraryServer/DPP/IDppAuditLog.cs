using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Tamper-evident, append-only audit log for DPP create/read/modify/delete operations
    /// (EN 18246 §4.7). Entries are hash-chained and bound to the acting operator id, making
    /// modifications non-repudiable and retrospective tampering detectable.
    /// </summary>
    public interface IDppAuditLog
    {
        /// <summary>
        /// Appends an entry for <paramref name="operation"/> on <paramref name="dppId"/> (optionally a
        /// specific <paramref name="elementPath"/>) by <paramref name="operatorId"/>, chaining it to the
        /// current tail.
        /// </summary>
        /// <exception cref="DppAuditException">
        /// Thrown when the entry cannot be durably committed. The originating operation must then fail:
        /// returning success for an access or change that was never logged would silently forfeit the
        /// non-repudiation guarantee this log exists to provide.
        /// </exception>
        /// <param name="operationId">
        /// Correlates a write-ahead <c>Attempted</c> entry with the entry recording its outcome.
        /// Supply the same value for both so an attempt with no matching outcome can be identified
        /// unambiguously, even when operations on the same DPP interleave. Use
        /// <see cref="NewOperationId"/> to generate one.
        /// </param>
        Task RecordAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome, string operationId = null);

        /// <summary>
        /// Generates an identifier correlating the two entries of one write-ahead audited operation.
        /// </summary>
        static string NewOperationId() => Guid.NewGuid().ToString("N");

        /// <summary>
        /// Recomputes the hash chain and returns true when every entry's hash matches its content and
        /// predecessor, and the chain still matches the persisted checkpoint, i.e. no entry was
        /// inserted, removed (including from the end) or altered.
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancels the verification. This reads the entire append-only audit table, so a caller that
        /// has given up (a timed-out health probe, a disconnected client) must be able to stop the
        /// scan; otherwise repeated probes accumulate increasingly expensive abandoned reads as the
        /// table grows.
        /// </param>
        Task<bool> VerifyChainAsync(CancellationToken cancellationToken = default);
    }
}
