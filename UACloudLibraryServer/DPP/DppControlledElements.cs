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
    /// reserved <c>controlledElements</c> object maps each <c>dictionaryReference</c> to the role (or
    /// roles) permitted to read the elements that carry it (EN 18239 §5.2). Keeping the mapping with the
    /// DPP values means roles are assigned per DPP at upload time rather than server-wide.
    /// </summary>
    public static class DppControlledElements
    {
        /// <summary>The reserved values-JSON property carrying the per-DPP access mapping.</summary>
        public const string PropertyName = "controlledElements";

        /// <summary>
        /// Parses the <c>controlledElements</c> object out of a DPP values JSON string into a
        /// case-insensitive map of <c>dictionaryReference</c> to its permitted roles. Returns an empty
        /// map when the values JSON is absent, malformed, or carries no mapping (⇒ all elements public).
        /// Each entry's value may be a single role string or an array of role strings.
        /// </summary>
        public static IReadOnlyDictionary<string, string[]> Parse(string valuesJson)
        {
            var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(valuesJson))
            {
                return map;
            }

            JsonNode root;
            try
            {
                root = JsonNode.Parse(valuesJson);
            }
            catch (JsonException)
            {
                return map;
            }

            if (root is not JsonObject obj || FindProperty(obj, PropertyName) is not JsonObject controlled)
            {
                return map;
            }

            foreach (KeyValuePair<string, JsonNode> entry in controlled)
            {
                string[] roles = ParseRoles(entry.Value);
                if (!string.IsNullOrWhiteSpace(entry.Key) && roles.Length > 0)
                {
                    map[entry.Key] = roles;
                }
            }

            return map;
        }

        /// <summary>
        /// Produces a values JSON string from freshly browsed node values, re-attaching the
        /// <c>controlledElements</c> mapping carried by <paramref name="existingValuesJson"/> so it
        /// survives value rewrites (browse-and-persist drops anything that is not a node value).
        /// </summary>
        public static string Merge(string nodeValuesJson, string existingValuesJson)
        {
            JsonObject result = ParseObject(nodeValuesJson);

            // Node browses never produce this key, but strip any stray copy before re-attaching.
            RemoveProperty(result, PropertyName);

            JsonObject existing = ParseObject(existingValuesJson);
            JsonNode controlled = FindProperty(existing, PropertyName);
            if (controlled is not null)
            {
                // DeepClone detaches the node from its current parent so it can be re-parented.
                result[PropertyName] = controlled.DeepClone();
            }

            return result.ToJsonString();
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

        // Case-insensitive property lookup (System.Text.Json object access is ordinal by default).
        private static JsonNode FindProperty(JsonObject obj, string name)
        {
            foreach (KeyValuePair<string, JsonNode> entry in obj)
            {
                if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Value;
                }
            }

            return null;
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
