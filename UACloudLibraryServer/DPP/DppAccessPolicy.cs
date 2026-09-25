using System;
using System.Collections.Generic;
using System.Linq;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Stateless evaluator for the per-DPP <c>controlledElements</c> mapping. The mapping is keyed by
    /// element <b>path</b>: the dotted chain of <c>elementId</c> values as emitted by reads, e.g.
    /// <c>6f9619ff-8b86-d011-b42d-00cf4fc964ff.3f2504e0-4f89-11d3-9a0c-0305e82c3301</c>. Those ids are
    /// GUIDs derived from each node's ExpandedNodeId (see <c>DPPService.BuildElementId</c>), <b>not</b>
    /// semantic BrowseNames - a name-based key such as <c>materials.supplierFacilityId</c> will never
    /// match an emitted path, so the element would silently stay public. Take the ids from a read of
    /// the DPP rather than composing them by hand.
    /// <para>
    /// The path is the element's address - not its <c>dictionaryReference</c> (which is reserved for
    /// semantic dictionary references such as IEC CDD per EN 18223 &#167;4.3). An element is public
    /// unless one of its ancestor-or-self path prefixes appears in the mapping; controlling a
    /// container path therefore controls its entire subtree. Controlled elements require one of the
    /// mapped roles (or an administrator). The mapping is supplied per call because it travels with
    /// each DPP's values JSON (see <see cref="DppControlledElements"/>).
    /// </para>
    /// </summary>
    public class DppAccessPolicy : IDppAccessPolicy
    {
        public bool IsPublic(string elementPath, IReadOnlyDictionary<string, string[]> controlled)
        {
            if (string.IsNullOrWhiteSpace(elementPath) || controlled is null || controlled.Count == 0)
            {
                return true;
            }

            foreach (string prefix in Prefixes(elementPath))
            {
                if (controlled.ContainsKey(prefix))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanRead(string elementPath, IEnumerable<string> callerRoles, IReadOnlyDictionary<string, string[]> controlled)
        {
            if (string.IsNullOrWhiteSpace(elementPath) || controlled is null || controlled.Count == 0)
            {
                return true;
            }

            string[] roles = callerRoles as string[] ?? callerRoles?.ToArray() ?? Array.Empty<string>();

            // Every controlled prefix on the path (the element itself and any controlled ancestor
            // container) must be satisfied by one of the caller's roles.
            foreach (string prefix in Prefixes(elementPath))
            {
                if (controlled.TryGetValue(prefix, out string[] allowed))
                {
                    bool satisfied = roles.Any(r =>
                        string.Equals(r, Roles.Administrator, StringComparison.OrdinalIgnoreCase) ||
                        allowed.Any(a => string.Equals(r, a, StringComparison.OrdinalIgnoreCase)));
                    if (!satisfied)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Yields each ancestor-or-self prefix of a dotted element path: "a", "a.b", "a.b.c".
        private static IEnumerable<string> Prefixes(string path)
        {
            int start = 0;
            while (true)
            {
                int dot = path.IndexOf('.', start);
                if (dot < 0)
                {
                    yield return path;
                    yield break;
                }

                yield return path.Substring(0, dot);
                start = dot + 1;
            }
        }
    }
}
