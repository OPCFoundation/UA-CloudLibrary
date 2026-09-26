using System;
using System.Threading.Tasks;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Persistence boundary for DPP version snapshots.
    /// </summary>
    /// <remarks>
    /// EN 18221 Clause 4.2 requires that all changes to the DPP "shall be archived"
    /// and that the version active at a given point in time "shall be retrievable by
    /// authenticated and authorised actors". The Lifecycle service snapshots the DPP through this
    /// abstraction before every successful update, so the snapshot history backs
    /// <c>ReadDPPVersionByIdAndDate</c>.
    /// </remarks>
    public interface IDppVersionArchive
    {
        /// <summary>
        /// Persists a snapshot of <paramref name="snapshot"/> for the given DPP id at <paramref name="capturedAtUtc"/>,
        /// together with the <c>controlledElements</c> access policy in force at that moment.
        /// </summary>
        /// <param name="controlledElementsValuesJson">
        /// The DPP's values JSON carrying the access mapping. Archived verbatim so the snapshot can
        /// later be filtered against the policy that applied to it rather than today's, which would
        /// otherwise disclose elements that were controlled then and un-mapped since.
        /// </param>
        /// <returns>
        /// <c>true</c> when the snapshot was durably stored, <c>false</c> otherwise. Callers
        /// implementing the EN 18221 Clause 4.2 archival guarantee MUST treat <c>false</c> as a
        /// hard failure of the surrounding update operation - returning success in that case
        /// would silently violate the archival contract that <c>ReadDPPVersionByIdAndDate</c>
        /// relies on.
        /// </returns>
        Task<bool> ArchiveAsync(string dppId, DigitalProductPassport snapshot, string controlledElementsValuesJson, DateTimeOffset capturedAtUtc);

        /// <summary>
        /// Returns the snapshot that was active at or before <paramref name="asOfUtc"/>, or <c>null</c>
        /// when no snapshot exists at that point in time.
        /// </summary>
        Task<DppVersionSnapshot> GetVersionAtAsync(string dppId, DateTimeOffset asOfUtc);
    }
}
