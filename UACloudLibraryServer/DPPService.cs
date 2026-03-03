using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    public class DPPService
    {
        // placeholder implementation for DPP storage and retrieval, to be replaced with actual database access in the future
        private readonly ConcurrentDictionary<string, JsonObject> _byDppId = new();
        private readonly ConcurrentDictionary<string, string> _dppIdByProductId = new();

        public JsonObject GetByDppId(string dppId) => _byDppId.TryGetValue(dppId, out var dpp) ? dpp : null;

        public JsonObject GetByProductId(string productId) => _dppIdByProductId.TryGetValue(productId, out var dppId) ? GetByDppId(dppId) : null;

        public IReadOnlyList<string> GetDppIdsByProductIds(IReadOnlyList<string> productIds)
        {
            var result = new List<string>();

            foreach (var pid in productIds)
            {
                if (_dppIdByProductId.TryGetValue(pid, out var dppId))
                {
                    result.Add(dppId);
                }
            }

            return result;
        }

        public JsonNode GetElement(string dppId, string elementPath)
        {
            if (!_byDppId.TryGetValue(dppId, out var dpp))
            {
                return null;
            }

            // Here we implement a simple "dot path" resolver (e.g., "a.b.c") as a pragmatic placeholder.
            return PathResolver.TryGet(dpp, elementPath);
        }

        public JsonObject GetDppVersionByIdAndDate(string dppId, DateTime timestamp)
        {
            if (_byDppId.TryGetValue(dppId, out var dpp))
            {
                // Use timestamp parameter once version history is implemented
                _ = timestamp;
                return dpp;
            }

            return null;
        }
    }
}
