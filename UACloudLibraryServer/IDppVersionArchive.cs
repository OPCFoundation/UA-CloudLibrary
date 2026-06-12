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
        /// Persists a snapshot of <paramref name="snapshot"/> for the given DPP id at <paramref name="capturedAtUtc"/>.
        /// </summary>
        Task ArchiveAsync(string dppId, DigitalProductPassport snapshot, DateTimeOffset capturedAtUtc);

        /// <summary>
        /// Returns the snapshot that was active at or before <paramref name="asOfUtc"/>, or <c>null</c>
        /// when no snapshot exists at that point in time.
        /// </summary>
        Task<DigitalProductPassport> GetVersionAtAsync(string dppId, DateTimeOffset asOfUtc);
    }
}
