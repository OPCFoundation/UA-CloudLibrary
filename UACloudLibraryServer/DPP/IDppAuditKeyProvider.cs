using System.Threading;
using System.Threading.Tasks;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// An audit key together with the fingerprint identifying it.
    /// </summary>
    /// <param name="Key">The key bytes, or <c>null</c> when no key is configured.</param>
    /// <param name="KeyId">
    /// Stable fingerprint of <paramref name="Key"/>, or <c>null</c> when there is no key. Derived
    /// from the key material rather than assigned by configuration, so it cannot be mislabelled and
    /// needs no additional operator input.
    /// </param>
    public readonly record struct DppAuditKey(byte[] Key, string KeyId);

    /// <summary>
    /// Outcome of resolving the key a historical entry was signed with.
    /// </summary>
    /// <param name="Found">
    /// Whether the required key is available. <c>false</c> means the entry cannot be verified
    /// because its key is not configured &#8212; emphatically not the same as the entry having been
    /// tampered with, and it must not be reported as such.
    /// </param>
    /// <param name="Key">
    /// The resolved key, or <c>null</c> for an entry written while unkeyed (which verifies with a
    /// bare hash).
    /// </param>
    public readonly record struct DppAuditKeyLookup(bool Found, byte[] Key);

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

        /// <summary>
        /// Returns the key new entries should be signed with, together with its fingerprint.
        /// </summary>
        Task<DppAuditKey> GetCurrentKeyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the key identified by <paramref name="keyId"/> so a historical entry can be
        /// verified with the key that actually signed it, rather than with whatever key happens to
        /// be configured now.
        /// </summary>
        /// <param name="keyId">
        /// Fingerprint recorded on the entry, or <c>null</c> for entries written while unkeyed.
        /// </param>
        Task<DppAuditKeyLookup> TryGetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default);
    }
}
