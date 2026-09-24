using System.Threading;
using System.Threading.Tasks;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Supplies the private key used to sign ESDCs. Resolution order is: the configured key, then a
    /// previously persisted server-generated key, then a freshly generated key which is persisted for
    /// subsequent use.
    /// </summary>
    /// <remarks>
    /// The key must be stable. It is the sole basis on which a verifier decides an ESDC is authentic,
    /// so a key that changes between restarts or differs between instances silently invalidates every
    /// ESDC issued under the previous key.
    /// </remarks>
    public interface IEsdcSigningKeyProvider
    {
        /// <summary>
        /// Returns the PKCS#8 private key PEM to sign with, generating and persisting one if the
        /// deployment has neither configured nor previously generated a key.
        /// </summary>
        Task<string> GetOrCreatePrivateKeyPemAsync(CancellationToken cancellationToken = default);
    }
}
