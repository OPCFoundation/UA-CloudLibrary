using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Helpers for the per-DPP <c>controlledElements</c> mapping that travels inside a DPP's values
    /// JSON. The values file is otherwise a flat <c>{ nodeId: value }</c> dictionary; an optional
    /// reserved <c>controlledElements</c> object maps each element <b>path</b> to the role (or roles)
    /// permitted to read that element and its subtree (EN 18239 &#167;5.2).
    /// <para>
    /// The path is the dotted <c>elementId</c> chain that also addresses the element in the API, and
    /// those ids are <b>GUIDs</b> derived from each node's ExpandedNodeId by
    /// <c>DPPService.BuildElementId</c> - not semantic BrowseNames. A mapping therefore looks like:
    /// </para>
    /// <code>
    /// "controlledElements": {
    ///   "6f9619ff-8b86-d011-b42d-00cf4fc964ff": "Auditor",
    ///   "6f9619ff-8b86-d011-b42d-00cf4fc964ff.3f2504e0-4f89-11d3-9a0c-0305e82c3301": [ "Auditor", "Recycler" ]
    /// }
    /// </code>
    /// <para>
    /// A name-based key such as <c>materials.supplierFacilityId</c> will never match an emitted path,
    /// so the element would remain public with no error raised. Copy the ids from a read of the DPP
    /// rather than composing them from BrowseNames.
    /// </para>
    /// <para>
    /// Note this is the element's address, not its <c>dictionaryReference</c> (reserved for semantic
    /// dictionary references such as IEC CDD per EN 18223 &#167;4.3). Keeping the mapping with the DPP
    /// values means roles are assigned per DPP at upload time rather than server-wide.
    /// </para>
    /// </summary>
    public static class DppControlledElements
    {
        /// <summary>The reserved values-JSON property carrying the per-DPP access mapping.</summary>
        public const string PropertyName = "controlledElements";

        /// <summary>
        /// Outcome of reading the reserved mapping. <see cref="Absent"/> and <see cref="Valid"/> are
        /// authoritative answers; <see cref="Invalid"/> means the mapping could not be understood and
        /// therefore says nothing about which elements are controlled.
        /// </summary>
        public enum MappingState
        {
            /// <summary>No mapping is present, so every element is public.</summary>
            Absent,

            /// <summary>A well-formed mapping was read.</summary>
            Valid,

            /// <summary>The values blob or the mapping itself is malformed and cannot be trusted.</summary>
            Invalid
        }

        /// <summary>
        /// The parsed mapping together with its <see cref="MappingState"/>. Callers must treat
        /// <see cref="MappingState.Invalid"/> as deny-all rather than as an empty (public) mapping:
        /// an unreadable access policy is a failure to determine access, not evidence of its absence.
        /// </summary>
        public sealed class MappingResult
        {
            internal MappingResult(MappingState state, IReadOnlyDictionary<string, string[]> entries)
            {
                State = state;
                Entries = entries;
            }

            public MappingState State { get; }

            public IReadOnlyDictionary<string, string[]> Entries { get; }

            /// <summary>True when the mapping could not be read and access must be denied.</summary>
            public bool IsInvalid => State == MappingState.Invalid;
        }

        /// <summary>
        /// Parses the <c>controlledElements</c> object out of a DPP values JSON string into a
        /// case-insensitive map of element path to its permitted roles, reporting whether the mapping
        /// was absent, valid, or malformed. Each entry's value may be a single role string or an array
        /// of role strings; an entry that names no usable role makes the whole mapping
        /// <see cref="MappingState.Invalid"/>, because silently dropping it would publish an element
        /// that the author intended to control.
        /// </summary>
        public static MappingResult Read(string valuesJson)
        {
            var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            // A DPP with no stored values has no elements to protect.
            if (string.IsNullOrWhiteSpace(valuesJson))
            {
                return new MappingResult(MappingState.Absent, map);
            }

            JsonNode root;
            try
            {
                root = JsonNode.Parse(valuesJson);
            }
            catch (JsonException)
            {
                // The blob that would carry the mapping is unreadable, so we cannot conclude the
                // mapping is absent - any controlled element it declared is now unknown to us.
                return new MappingResult(MappingState.Invalid, map);
            }

            if (root is not JsonObject obj)
            {
                return new MappingResult(MappingState.Invalid, map);
            }

            // Distinguish a missing property from one explicitly set to JSON null. JsonNode.Parse
            // represents a null value as a null reference, so a single null check would treat
            // { "controlledElements": null } as "no mapping" - i.e. public-by-default - turning a
            // malformed policy into a fail-open one.
            if (!TryFindProperty(obj, PropertyName, out JsonNode controlledNode))
            {
                return new MappingResult(MappingState.Absent, map);
            }

            if (controlledNode is not JsonObject controlled)
            {
                // Covers an explicit null as well as a non-object value.
                return new MappingResult(MappingState.Invalid, map);
            }

            foreach (KeyValuePair<string, JsonNode> entry in controlled)
            {
                string[] roles = ParseRoles(entry.Value);
                if (string.IsNullOrWhiteSpace(entry.Key) || roles.Length == 0)
                {
                    return new MappingResult(MappingState.Invalid, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));
                }

                map[entry.Key] = roles;
            }

            return new MappingResult(MappingState.Valid, map);
        }

        /// <summary>
        /// Produces a values JSON string from freshly browsed node values, re-attaching the
        /// <c>controlledElements</c> mapping carried by <paramref name="existingValuesJson"/> so it
        /// survives value rewrites (browse-and-persist drops anything that is not a node value).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="existingValuesJson"/> is present but unreadable. Rewriting it
        /// would silently discard an access mapping we cannot see, turning controlled elements public.
        /// </exception>
        public static string Merge(string nodeValuesJson, string existingValuesJson)
        {
            JsonObject result = ParseObject(nodeValuesJson);

            // Node browses never produce this key, but strip any stray copy before re-attaching.
            RemoveProperty(result, PropertyName);

            if (!string.IsNullOrWhiteSpace(existingValuesJson) && TryParseObject(existingValuesJson) is null)
            {
                throw new InvalidOperationException(
                    "Existing DPP values JSON is malformed; refusing to rewrite it because that would discard any controlled-element mapping it carries.");
            }

            JsonObject existing = ParseObject(existingValuesJson);
            if (TryFindProperty(existing, PropertyName, out JsonNode controlled))
            {
                if (controlled is null)
                {
                    // An explicit null is an unreadable mapping, which Read reports as Invalid
                    // (deny-all). Rewriting it away would silently downgrade the DPP to public.
                    throw new InvalidOperationException(
                        $"Existing DPP values JSON sets '{PropertyName}' to null, which is not a valid access mapping; refusing to rewrite it because that would turn controlled elements public.");
                }

                // DeepClone detaches the node from its current parent so it can be re-parented.
                result[PropertyName] = controlled.DeepClone();
            }

            return result.ToJsonString();
        }

        private static JsonObject TryParseObject(string json)
        {
            try
            {
                return JsonNode.Parse(json) as JsonObject;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static JsonObject ParseObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new JsonObject();
            }

            try
            {
                return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                return new JsonObject();
            }
        }

        /// <summary>
        /// Case-insensitive property lookup that reports presence separately from the value, so an
        /// explicit JSON null (which surfaces as a null <see cref="JsonNode"/>) is not mistaken for
        /// an absent property.
        /// </summary>
        private static bool TryFindProperty(JsonObject obj, string name, out JsonNode value)
        {
            foreach (KeyValuePair<string, JsonNode> entry in obj)
            {
                if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static void RemoveProperty(JsonObject obj, string name)
        {
            string key = null;
            foreach (KeyValuePair<string, JsonNode> entry in obj)
            {
                if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    key = entry.Key;
                    break;
                }
            }

            if (key is not null)
            {
                obj.Remove(key);
            }
        }

        private static string[] ParseRoles(JsonNode token)
        {
            var roles = new List<string>();

            if (token is JsonArray array)
            {
                foreach (JsonNode item in array)
                {
                    if (item is JsonValue arrayValue && arrayValue.TryGetValue(out string role) && !string.IsNullOrWhiteSpace(role))
                    {
                        roles.Add(role);
                    }
                }
            }
            else if (token is JsonValue value && value.TryGetValue(out string single) && !string.IsNullOrWhiteSpace(single))
            {
                roles.Add(single);
            }

            return roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
