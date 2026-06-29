using System.Collections.Generic;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Evaluates whether a DPP data element is public or controlled, and whether a caller holding a
    /// given set of roles may read it, against a per-DPP <paramref name="controlled"/> mapping
    /// (dictionaryReference -> permitted roles). The mapping travels inside the DPP's values JSON
    /// (see <see cref="DppControlledElements"/>), matching the EN 18223 §4.3 / EN 18239 §5.2 layering
    /// where the per-element access rule lives with the data rather than in the DPP payload schema.
    /// Public is the default so unauthenticated read of public DPP data works without login.
    /// </summary>
    public interface IDppAccessPolicy
    {
        /// <summary>
        /// True when the element carrying <paramref name="dictionaryReference"/> is public, i.e.
        /// readable without authentication. A null/empty reference, or one absent from
        /// <paramref name="controlled"/>, is public.
        /// </summary>
        bool IsPublic(string dictionaryReference, IReadOnlyDictionary<string, string[]> controlled);

        /// <summary>
        /// True when a caller holding <paramref name="callerRoles"/> may read the element carrying
        /// <paramref name="dictionaryReference"/> given the per-DPP <paramref name="controlled"/> map.
        /// Public elements are readable by anyone (including anonymous callers); controlled elements
        /// require one of the mapped roles. Administrators may read everything.
        /// </summary>
        bool CanRead(string dictionaryReference, IEnumerable<string> callerRoles, IReadOnlyDictionary<string, string[]> controlled);
    }
}
