using System;
using System.Collections.Generic;
using System.Linq;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Stateless evaluator for the per-DPP <c>controlledElements</c> mapping. An element is public when
    /// its <c>dictionaryReference</c> is null/empty or absent from the mapping; otherwise it is
    /// controlled and the caller must hold one of the mapped roles (or be an administrator). The mapping
    /// is supplied per call because it travels with each DPP's values JSON
    /// (see <see cref="DppControlledElements"/>), so the access rule stays in the data/dictionary layer
    /// (EN 18223 §4.3, EN 18239 §5.2) rather than being baked into the DPP payload schema.
    /// </summary>
    public class DppAccessPolicy : IDppAccessPolicy
    {
        public bool IsPublic(string dictionaryReference, IReadOnlyDictionary<string, string[]> controlled)
        {
            if (string.IsNullOrWhiteSpace(dictionaryReference) || controlled is null)
            {
                return true;
            }

            return !controlled.ContainsKey(dictionaryReference);
        }

        public bool CanRead(string dictionaryReference, IEnumerable<string> callerRoles, IReadOnlyDictionary<string, string[]> controlled)
        {
            if (IsPublic(dictionaryReference, controlled))
            {
                return true;
            }

            if (callerRoles is null)
            {
                return false;
            }

            string[] allowed = controlled[dictionaryReference];
            return callerRoles.Any(r =>
                string.Equals(r, Roles.Administrator, StringComparison.OrdinalIgnoreCase) ||
                allowed.Any(a => string.Equals(r, a, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
