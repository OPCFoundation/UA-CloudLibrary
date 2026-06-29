using System.Collections.Generic;
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
        /// current tail. Logging never throws into the request path: failures are swallowed/logged.
        /// </summary>
        Task RecordAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome);

        /// <summary>
        /// Recomputes the hash chain and returns true when every entry's hash matches its content and
        /// predecessor, i.e. no entry was inserted, removed or altered.
        /// </summary>
        Task<bool> VerifyChainAsync();
    }
}
