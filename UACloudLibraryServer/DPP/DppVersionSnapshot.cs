using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// An archived DPP version together with the access policy that was in force when it was
    /// captured.
    /// </summary>
    /// <remarks>
    /// The controlled-elements mapping lives in the DPP's values blob, which only ever holds the
    /// <i>current</i> state. Filtering a historical snapshot against that current mapping would
    /// expose an element that was controlled at the requested date but has since been un-mapped, so
    /// the policy has to be archived alongside the data it governs and applied as of that version.
    /// </remarks>
    public sealed class DppVersionSnapshot
    {
        public DppVersionSnapshot(DigitalProductPassport dpp, string controlledElementsValuesJson, bool policyArchived)
        {
            Dpp = dpp;
            ControlledElementsValuesJson = controlledElementsValuesJson;
            PolicyArchived = policyArchived;
        }

        /// <summary>The DPP as it stood at the archived point in time.</summary>
        public DigitalProductPassport Dpp { get; }

        /// <summary>
        /// The values JSON carrying the <c>controlledElements</c> mapping as it stood when this
        /// version was captured. Parsed with <see cref="DppControlledElements.Read(string)"/>.
        /// </summary>
        public string ControlledElementsValuesJson { get; }

        /// <summary>
        /// False for snapshots written before the archive recorded access policy. Their policy is
        /// unknowable after the fact, so callers must fail closed rather than substituting today's
        /// mapping - the substitution is exactly the disclosure this type exists to prevent.
        /// </summary>
        public bool PolicyArchived { get; }
    }
}
