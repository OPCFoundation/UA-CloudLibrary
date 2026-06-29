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
        /// Verifies that the ESDC's signature matches its data and was produced by the configured
        /// signing key. Returns false for any tampering or signature mismatch.
        /// </summary>
        bool Verify(ElectronicSignedDataConstruct esdc);
    }
}
