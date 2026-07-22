using System.Collections.Generic;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Evaluates whether a DPP data element is public or controlled, and whether a caller holding a
    /// given set of roles may read it, against a per-DPP <paramref name="controlled"/> mapping
    /// (element path -> permitted roles). The key is the element's address (its dotted <c>elementId</c>
    /// path, the same addressing used by the <c>elementIdPath</c> API), NOT its
    /// <see cref="DataElement.DictionaryReference"/> — that attribute is reserved for semantic dictionary
    /// references (e.g. IEC CDD, ECLASS, EU Vocabularies) per EN 18223 §4.3. A controlled path also
    /// controls its whole subtree (prefix match). The mapping travels inside the DPP's values JSON
    /// (see <see cref="DppControlledElements"/>). Public is the default so unauthenticated read of public
    /// DPP data works without login.
    /// </summary>
    public interface IDppAccessPolicy
    {
        /// <summary>
        /// True when the element at <paramref name="elementPath"/> is public, i.e. readable without
        /// authentication. A null/empty path, or one with no controlled ancestor-or-self prefix in
        /// <paramref name="controlled"/>, is public.
        /// </summary>
        bool IsPublic(string elementPath, IReadOnlyDictionary<string, string[]> controlled);

        /// <summary>
        /// True when a caller holding <paramref name="callerRoles"/> may read the element at
        /// <paramref name="elementPath"/> given the per-DPP <paramref name="controlled"/> map. Every
        /// controlled prefix on the path must be satisfied by one of the caller's roles. Public elements
        /// are readable by anyone (including anonymous callers). Administrators may read everything.
        /// </summary>
        bool CanRead(string elementPath, IEnumerable<string> callerRoles, IReadOnlyDictionary<string, string[]> controlled);
    }
}
