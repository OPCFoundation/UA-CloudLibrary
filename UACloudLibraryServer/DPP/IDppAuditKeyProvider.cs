using System.Threading;
using System.Threading.Tasks;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Supplies the key used to authenticate DPP audit entries.
    /// </summary>
    /// <remarks>
    /// The audit chain is stored in the same database it is meant to protect, so an unkeyed digest
    /// proves nothing against anyone able to write to that database: the hashes are a public
    /// function of the rows, and can simply be recomputed after an edit. Keying the digest means an
    /// attacker additionally needs this key, which is deliberately held outside the database.
    /// </remarks>
    public interface IDppAuditKeyProvider
    {
        /// <summary>
        /// Returns the key used to authenticate audit entries, or null when no key is configured and
        /// the log is therefore only integrity-checked rather than authenticated.
        /// </summary>
        Task<byte[]> GetAuditKeyAsync(CancellationToken cancellationToken = default);
    }
}
