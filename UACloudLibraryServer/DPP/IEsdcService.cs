using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Issues and verifies Electronic Signed Data Constructs over DPPs (EN 18246 §4.5). Verification
    /// must be possible free of charge and unrestricted, so verifiers only need the public key.
    /// </summary>
    public interface IEsdcService
    {
        /// <summary>
        /// Produces an ESDC binding the DPP's data to the economic operator (issuer) and product
        /// (subject) using a digital signature.
        /// </summary>
        ElectronicSignedDataConstruct Issue(DigitalProductPassport dpp);

        /// <summary>
        /// Verifies that the ESDC's signature matches its data, that it was produced by a trusted
        /// signing key (this instance's own key or a configured trust anchor), and that the envelope
        /// metadata agrees with the signed credential. Returns false for any tampering, signature
        /// mismatch, or untrusted signer.
        /// </summary>
        /// <remarks>
        /// A key carried inside the ESDC is never trusted for this decision: anyone can mint a key
        /// pair, sign their own credential, and embed the matching public key, so honouring it would
        /// verify self-consistency rather than authenticity.
        /// </remarks>
        bool Verify(ElectronicSignedDataConstruct esdc);
    }
}
