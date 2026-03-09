using System;
using System.Text.Json.Nodes;

namespace Opc.Ua.Cloud.Library.Models
{
    public static class PathResolver
    {
        // Simple dot-separated path: "a.b.c"
        public static JsonNode TryGet(JsonNode root, string path)
        {
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            JsonNode cur = root;

            foreach (var part in parts)
            {
                if (cur is JsonObject obj)
                {
                    cur = obj[part];
                }
                else
                {
                    return null;
                }
            }

            return cur;
        }

        public static void Set(JsonNode root, string path, JsonNode value)
        {
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) throw new ArgumentException("Empty path", nameof(path));

            JsonNode cur = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (cur is not JsonObject obj)
                    throw new InvalidOperationException("Path traversed into a non-object node.");

                var next = obj[parts[i]];
                if (next is not JsonObject nextObj)
                {
                    nextObj = new JsonObject();
                    obj[parts[i]] = nextObj;
                }
                cur = nextObj;
            }

            if (cur is not JsonObject parent)
                throw new InvalidOperationException("Cannot set value on non-object.");

            parent[parts[^1]] = value;
        }
    }
}
