using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    public class DPPService
    {
        private readonly ILogger _logger;
        private readonly UAClient _client;
        private readonly CloudLibDataProvider _dataProvider;
        private readonly DbFileStorage _storage;
        private readonly IDppVersionArchive _archive;

        public DPPService(UAClient client, CloudLibDataProvider dataProvider, DbFileStorage storage, IDppVersionArchive archive, ILoggerFactory loggerFactory)
        {
            _client = client;
            _dataProvider = dataProvider;
            _storage = storage;
            _archive = archive;
            _logger = loggerFactory.CreateLogger("DPPService");
        }

        public async Task<DigitalProductPassport> GetByDppId(string userId, string dppId)
        {
            DigitalProductPassport dpp = await BrowseDppFromRootAsync(userId, dppId).ConfigureAwait(false);
            if (dpp != null)
            {
                return dpp;
            }

            _logger.LogWarning("DPP not found for id {DppId}", dppId);
            return null;
        }

        /// <summary>
        /// Returns the DPP snapshot that was active at <paramref name="asOfUtc"/>, per
        /// EN 18222 (Method ReadDPPVersionByIdAndDate). If the requested
        /// timestamp is at or after the current server time, the live DPP is returned; otherwise
        /// the archive is consulted for the latest snapshot at or before that timestamp.
        /// Returns <c>null</c> when no version of the DPP existed at the requested point in time.
        /// </summary>
        public async Task<DigitalProductPassport> GetDppVersionByIdAndDate(string userId, string dppId, DateTimeOffset asOfUtc)
        {
            DateTimeOffset target = asOfUtc.ToUniversalTime();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (target >= now)
            {
                return await GetByDppId(userId, dppId).ConfigureAwait(false);
            }

            DigitalProductPassport archived = await _archive.GetVersionAtAsync(dppId, target).ConfigureAwait(false);
            if (archived != null)
            {
                return archived;
            }

            // No archived snapshot: only the live DPP is available. The spec wants the version
            // valid at the requested date, so return null when nothing was archived for that time.
            return null;
        }

        public async Task<DigitalProductPassport> GetByProductId(string userId, string productId)
        {
            List<ObjectModel> dppList = _dataProvider.GetNodeModels(nsm => nsm.Objects, userId)
                .Where(nsm => (nsm.DisplayName != null) && (nsm.DisplayName.Count > 0) && (nsm.DisplayName[0].Text == "UniqueProductIdentifier") && (nsm.NodeId == productId))
                .ToList();

            if (dppList != null)
            {
                // return the latest DPP if multiple are found for the same product id
                DigitalProductPassport latestDpp = null;
                foreach (ObjectModel dpp in dppList)
                {
                    if (latestDpp == null)
                    {
                        latestDpp = await BrowseDppFromRootAsync(userId, dpp.NodeSet.Identifier).ConfigureAwait(false);
                    }
                    else
                    {
                        DigitalProductPassport currentDpp = await BrowseDppFromRootAsync(userId, dpp.NodeSet.Identifier).ConfigureAwait(false);
                        if ((currentDpp != null) && (currentDpp.LastUpdate > latestDpp.LastUpdate))
                        {
                            latestDpp = currentDpp;
                        }
                    }
                }

                return latestDpp;
            }

            _logger.LogWarning("DPP not found for product id {ProductId}", productId);
            return null;
        }

        public IReadOnlyList<string> GetDppIdsByProductIds(string userId, IReadOnlyList<string> productIds)
        {
            var result = new List<string>();

            foreach (string productId in productIds)
            {
                List<ObjectModel> dppList = _dataProvider.GetNodeModels(nsm => nsm.Objects, userId)
                .Where(nsm => (nsm.DisplayName != null) && (nsm.DisplayName.Count > 0) && (nsm.DisplayName[0].Text == "UniqueProductIdentifier") && (nsm.NodeId == productId))
                .ToList();

                if (dppList != null)
                {
                    foreach (ObjectModel dpp in dppList)
                    {
                        result.Add(dpp.NodeSet.Identifier);
                    }
                }
            }

            return result;
        }

        public async Task<(ElementResult Result, string ErrorMessage, DataElement Element)> GetElement(
            string userId, string dppId, string elementPath)
        {
            if (!DppJsonPath.TryParse(elementPath, out IReadOnlyList<DppJsonPath.Segment> segments, out string parseError))
            {
                return (ElementResult.BadRequest, parseError, null);
            }

            DigitalProductPassport dpp = await GetByDppId(userId, dppId).ConfigureAwait(false);
            if (dpp == null)
            {
                return (ElementResult.NotFound, null, null);
            }

            // The first segment may resolve against either the implicit "elements" collection
            // or a top-level scalar property. We expose only the elements tree via this method,
            // matching the EN 18222 contract (returns a DataElement).
            IReadOnlyList<DataElement> roots = dpp.Elements;
            int startIndex = 0;

            // Allow consumers to omit a leading "elements" segment for ergonomics; the OPC UA
            // tree is rooted at that collection in our model.
            if (segments[0].IsName && string.Equals(segments[0].Name, "elements", StringComparison.Ordinal))
            {
                startIndex = 1;
            }

            DataElement match = ResolveElement(roots, segments, startIndex);
            return match is null
                ? (ElementResult.NotFound, null, null)
                : (ElementResult.Success, null, match);
        }

        /// <summary>
        /// Outcome of <see cref="GetElement"/> calls.
        /// </summary>
        public enum ElementResult
        {
            Success,
            NotFound,
            BadRequest
        }

        // Walks a parsed JSONPath through the in-memory DataElement tree.
        private static DataElement ResolveElement(IReadOnlyList<DataElement> elements, IReadOnlyList<DppJsonPath.Segment> segments, int segmentIndex)
        {
            DataElement current = null;
            IReadOnlyList<DataElement> currentChildren = elements;

            for (int i = segmentIndex; i < segments.Count; i++)
            {
                if (currentChildren is null)
                {
                    return null;
                }

                DppJsonPath.Segment segment = segments[i];
                DataElement next = null;

                if (segment.IsIndex)
                {
                    int idx = segment.Index.Value;
                    if (idx >= 0 && idx < currentChildren.Count)
                    {
                        next = currentChildren[idx];
                    }
                }
                else
                {
                    foreach (DataElement candidate in currentChildren)
                    {
                        if (string.Equals(candidate.ElementId, segment.Name, StringComparison.Ordinal))
                        {
                            next = candidate;
                            break;
                        }
                    }
                }

                if (next is null)
                {
                    return null;
                }

                current = next;

                if (i == segments.Count - 1)
                {
                    return current;
                }

                currentChildren = current switch {
                    DataElementCollection coll => coll.Elements,
                    MultiValuedDataElement multi => multi.Value,
                    _ => null
                };
            }

            return current;
        }

        /// <summary>
        /// Result of an <see cref="UpdateDppById"/> call.
        /// </summary>
        public enum UpdateDppResult
        {
            Success,
            NotFound,
            BadRequest,
            WriteFailed
        }

        // JSON key (camelCase per DigitalProductPassport contract) -> OPC UA BrowseName (PascalCase per nodeset).
        // digitalProductPassportId is intentionally excluded: the DPP ID is immutable in an update.
        // contentSpecificationIds is intentionally excluded: it is a [0..*] list, not a scalar, and
        // updating it would require its own resolver (matching how 'elements' is handled).
        private static readonly Dictionary<string, string> s_dppScalarBrowseNames =
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["uniqueProductIdentifier"] = "UniqueProductIdentifier",
                ["granularity"] = "Granularity",
                ["dppSchemaVersion"] = "DppSchemaVersion",
                ["dppStatus"] = "DppStatus",
                ["lastUpdate"] = "LastUpdate",
                ["economicOperatorId"] = "EconomicOperatorId",
                ["facilityId"] = "FacilityId",
            };

        /// <summary>
        /// Applies a partial update to the DPP with the given ID per EN 18222 (Method UpdateDPPById).
        /// </summary>
        /// <remarks>
        /// The update has merge-patch-shaped semantics (RFC 7396): only the fields that appear
        /// in the request body are touched, and members that are absent from the body are left
        /// unchanged. The full RFC 7396 deletion rule (a JSON member with value <c>null</c> means
        /// "delete that field") is intentionally <b>not</b> implemented because the DPP is backed
        /// by a fixed OPC UA address space (nodes cannot be created or destroyed at runtime, and
        /// EN 18223 Clause 4.1.2.1 Table 1 marks most header fields as required with cardinality
        /// [1]). Consequently:
        /// <list type="bullet">
        ///   <item>An explicit <c>null</c> on any scalar field is rejected as <see cref="UpdateDppResult.BadRequest"/>.</item>
        ///   <item>The <c>elements</c> member must be a JSON array; <c>null</c> and any non-array value are rejected.</item>
        ///   <item>Within <c>elements</c>, each entry updates the leaf addressed by its <c>elementId</c>;
        ///         child <c>DataElement</c> nodes are never added or removed.</item>
        /// </list>
        /// The spec requires that "if the update of some parts fails the complete update process will fail
        /// and there should be no changes adopted in the DPP". OPC UA writes are not transactional, so this
        /// method performs an up-front resolution pass (locating every target node and validating field names)
        /// before issuing any writes, minimizing the risk of partial application. A snapshot of the current DPP
        /// state is captured to <see cref="IDppVersionArchive"/> immediately before writes are applied,
        /// satisfying the EN 18221 Clause 4.2 archiving requirement (durable archival depends on the configured
        /// <see cref="IDppVersionArchive"/> implementation).
        /// </remarks>
        public async Task<(UpdateDppResult Result, string ErrorMessage, DigitalProductPassport Updated)> UpdateDppById(
            string userId, string dppId, JsonObject partial)
        {
            if (partial is null)
            {
                return (UpdateDppResult.BadRequest, "Request body must contain a partial DPP object.", null);
            }

            // Locate the DPP root node in the OPC UA address space (same lookup pattern as the read path).
            List<NodesetViewerNode> rootChildren = await _client.GetChildren(userId, dppId, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (rootChildren == null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            NodesetViewerNode dppNode = rootChildren.FirstOrDefault(n => n.Text == "DigitalProductPassport");
            if (dppNode == null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            List<NodesetViewerNode> dppProperties = await _client.GetChildren(userId, dppId, dppNode.Id).ConfigureAwait(false);
            if (dppProperties == null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            // --- Phase 1: resolve every requested write to a concrete OPC UA node id; reject the whole patch on any mismatch. ---
            var pendingWrites = new List<(string NodeId, string Value, string FieldPath)>();

            foreach (KeyValuePair<string, JsonNode> entry in partial)
            {
                if (string.Equals(entry.Key, "digitalProductPassportId", StringComparison.Ordinal))
                {
                    return (UpdateDppResult.BadRequest, "digitalProductPassportId is immutable and cannot be updated.", null);
                }

                if (string.Equals(entry.Key, "elements", StringComparison.Ordinal))
                {
                    NodesetViewerNode elementsNode = dppProperties.FirstOrDefault(p => p.Text == "Elements");
                    if (elementsNode == null)
                    {
                        return (UpdateDppResult.BadRequest, "DPP does not expose an Elements collection.", null);
                    }

                    if (entry.Value is not JsonArray elementsArray)
                    {
                        // Deletion / clearing of the elements collection is not supported; see method remarks.
                        return (UpdateDppResult.BadRequest, "'elements' must be a JSON array (deletion via null is not supported).", null);
                    }

                    var (err, resolved) = await ResolveElementWritesAsync(userId, dppId, elementsNode, elementsArray, "elements").ConfigureAwait(false);
                    if (err != null)
                    {
                        return (UpdateDppResult.BadRequest, err, null);
                    }

                    pendingWrites.AddRange(resolved);
                    continue;
                }

                if (!s_dppScalarBrowseNames.TryGetValue(entry.Key, out string browseName))
                {
                    return (UpdateDppResult.BadRequest, $"Unknown DPP field: '{entry.Key}'.", null);
                }

                NodesetViewerNode target = dppProperties.FirstOrDefault(p => p.Text == browseName);
                if (target == null)
                {
                    return (UpdateDppResult.BadRequest, $"DPP does not expose field: '{entry.Key}'.", null);
                }

                // Reject explicit null on scalar fields: deletion is not supported (see method remarks).
                // Omitting the member from the request leaves the field unchanged, which is the spec-correct
                // way to "not touch" a field under merge-patch-shaped semantics.
                if (entry.Value is null)
                {
                    return (UpdateDppResult.BadRequest, $"Field '{entry.Key}' cannot be set to null; omit it from the request to leave it unchanged.", null);
                }

                pendingWrites.Add((target.Id, JsonNodeToWireValue(entry.Value), entry.Key));
            }

            // --- Phase 2: apply the resolved writes. Fail-fast on the first OPC UA write error. ---
            // Snapshot the current DPP before mutating so ReadDPPVersionByIdAndDate can serve the
            // pre-update version (EN 18221 Clause 4.2 archiving requirement).
            DigitalProductPassport preUpdate = await GetByDppId(userId, dppId).ConfigureAwait(false);
            if (preUpdate != null)
            {
                await _archive.ArchiveAsync(dppId, preUpdate, DateTimeOffset.UtcNow).ConfigureAwait(false);
            }

            foreach (var (nodeId, value, fieldPath) in pendingWrites)
            {
                bool ok = await _client.VariableWrite(userId, dppId, nodeId, value).ConfigureAwait(false);
                if (!ok)
                {
                    _logger.LogError("UpdateDppById: write failed for DPP {DppId}, field '{Field}'.", dppId, fieldPath);
                    return (UpdateDppResult.WriteFailed, $"Failed to write field '{fieldPath}'.", null);
                }
            }

            // Mirror BrowserController.Save: writes go to the live SimpleServer in-memory, so we
            // must explicitly snapshot the variable values back into DbFiles.Values for the change
            // to survive a session / server restart.
            if (!await PersistNodesetValuesAsync(userId, dppId).ConfigureAwait(false))
            {
                _logger.LogError("UpdateDppById: failed to persist updated values for DPP {DppId}.", dppId);
                return (UpdateDppResult.WriteFailed, "Update applied in memory but could not be persisted to storage.", null);
            }

            DigitalProductPassport updated = await GetByDppId(userId, dppId).ConfigureAwait(false);
            return (UpdateDppResult.Success, null, updated);
        }

        /// <summary>
        /// Updates a single leaf <see cref="DataElement"/> on a DPP, addressed by an RFC 9535 JSONPath,
        /// per EN 18222 (Method UpdateDataElement).
        /// </summary>
        /// <remarks>
        /// The path must refer to a single leaf element (a <c>SingleValuedDataElement</c> in our model).
        /// Targeting a <c>DataElementCollection</c> is a client error because the spec requires the update
        /// of "a specific data element" and the request body carries a single value/object payload, not an
        /// entire subtree shape. The pre-update DPP is archived through <see cref="IDppVersionArchive"/>
        /// before the write is applied, consistent with the archival contract used by
        /// <see cref="UpdateDppById"/>.
        /// </remarks>
        public async Task<(UpdateDppResult Result, string ErrorMessage, DataElement Updated)> UpdateDataElement(
            string userId, string dppId, string elementIdPath, JsonNode newValue)
        {
            if (newValue is null)
            {
                return (UpdateDppResult.BadRequest, "Request body must contain a value or element object.", null);
            }

            if (!DppJsonPath.TryParse(elementIdPath, out IReadOnlyList<DppJsonPath.Segment> segments, out string parseError))
            {
                return (UpdateDppResult.BadRequest, parseError, null);
            }

            // Locate the DPP root node (same pattern as UpdateDppById).
            List<NodesetViewerNode> rootChildren = await _client.GetChildren(userId, dppId, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (rootChildren == null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            NodesetViewerNode dppNode = rootChildren.FirstOrDefault(n => n.Text == "DigitalProductPassport");
            if (dppNode == null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            List<NodesetViewerNode> dppProperties = await _client.GetChildren(userId, dppId, dppNode.Id).ConfigureAwait(false);
            NodesetViewerNode elementsNode = dppProperties?.FirstOrDefault(p => p.Text == "Elements");
            if (elementsNode == null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            // Allow callers to omit a leading "elements" segment for ergonomics, matching the read path.
            int startIndex = 0;
            if (segments[0].IsName && string.Equals(segments[0].Name, "elements", StringComparison.Ordinal))
            {
                startIndex = 1;
            }

            if (startIndex >= segments.Count)
            {
                return (UpdateDppResult.BadRequest, "elementIdPath must address a specific element, not the elements root.", null);
            }

            // Walk the live OPC UA tree per JSONPath segment.
            NodesetViewerNode current = elementsNode;
            List<NodesetViewerNode> currentChildren = await _client.GetChildren(userId, dppId, current.Id).ConfigureAwait(false);

            for (int i = startIndex; i < segments.Count; i++)
            {
                if (currentChildren == null || currentChildren.Count == 0)
                {
                    return (UpdateDppResult.NotFound, null, null);
                }

                DppJsonPath.Segment segment = segments[i];
                NodesetViewerNode next = segment.IsIndex
                    ? (segment.Index.Value >= 0 && segment.Index.Value < currentChildren.Count ? currentChildren[segment.Index.Value] : null)
                    : currentChildren.FirstOrDefault(c => string.Equals(c.Text, segment.Name, StringComparison.Ordinal));

                if (next == null)
                {
                    return (UpdateDppResult.NotFound, null, null);
                }

                current = next;

                if (i < segments.Count - 1)
                {
                    currentChildren = await _client.GetChildren(userId, dppId, current.Id).ConfigureAwait(false);
                }
                else
                {
                    // Final segment: confirm this is a leaf, not a collection.
                    List<NodesetViewerNode> grandChildren = await _client.GetChildren(userId, dppId, current.Id).ConfigureAwait(false);
                    if (grandChildren != null && grandChildren.Count > 0)
                    {
                        return (UpdateDppResult.BadRequest,
                            "elementIdPath addresses a collection; updates must target a single leaf data element.",
                            null);
                    }
                }
            }

            // Accept either a bare value or the { "elementId": ..., "value": ... } shape from the spec.
            JsonNode payloadValue = newValue;
            if (newValue is JsonObject obj && obj.TryGetPropertyValue("value", out JsonNode valueProperty))
            {
                payloadValue = valueProperty;
            }

            // Snapshot before mutating so the previous version remains addressable (Clause 4.7).
            DigitalProductPassport preUpdate = await GetByDppId(userId, dppId).ConfigureAwait(false);
            if (preUpdate != null)
            {
                await _archive.ArchiveAsync(dppId, preUpdate, DateTimeOffset.UtcNow).ConfigureAwait(false);
            }

            bool ok = await _client.VariableWrite(userId, dppId, current.Id, JsonNodeToWireValue(payloadValue)).ConfigureAwait(false);
            if (!ok)
            {
                _logger.LogError("UpdateDataElement: write failed for DPP {DppId}, path '{Path}'.", dppId, elementIdPath);
                return (UpdateDppResult.WriteFailed, $"Failed to write element '{elementIdPath}'.", null);
            }

            // Persist values to DbFiles.Values so the write survives a session restart.
            if (!await PersistNodesetValuesAsync(userId, dppId).ConfigureAwait(false))
            {
                _logger.LogError("UpdateDataElement: failed to persist updated values for DPP {DppId}.", dppId);
                return (UpdateDppResult.WriteFailed, "Update applied in memory but could not be persisted to storage.", null);
            }

            (ElementResult readResult, _, DataElement updated) = await GetElement(userId, dppId, elementIdPath).ConfigureAwait(false);
            if (readResult != ElementResult.Success)
            {
                return (UpdateDppResult.WriteFailed, "Write succeeded but updated element could not be re-read.", null);
            }

            return (UpdateDppResult.Success, null, updated);
        }

        // Walks a partial 'elements' JSON array against the live OPC UA Elements subtree,
        // resolving each leaf write to a target node id. Returns (errorMessage, resolvedWrites).
        private async Task<(string Error, List<(string NodeId, string Value, string FieldPath)> Writes)> ResolveElementWritesAsync(
            string userId, string nodesetIdentifier, NodesetViewerNode parentNode, JsonArray partialElements, string pathPrefix)
        {
            var writes = new List<(string, string, string)>();

            List<NodesetViewerNode> liveChildren = await _client.GetChildren(userId, nodesetIdentifier, parentNode.Id).ConfigureAwait(false);
            if (liveChildren == null)
            {
                return ($"Element path '{pathPrefix}' is not browsable on the server.", null);
            }

            foreach (JsonNode item in partialElements)
            {
                if (item is not JsonObject elementObj)
                {
                    return ($"Each entry under '{pathPrefix}' must be a JSON object.", null);
                }

                if (!elementObj.TryGetPropertyValue("elementId", out JsonNode elementIdNode) || elementIdNode is null)
                {
                    return ($"Each entry under '{pathPrefix}' must carry an 'elementId'.", null);
                }

                string elementId = elementIdNode.GetValue<string>();
                NodesetViewerNode match = liveChildren.FirstOrDefault(c => c.Text == elementId);
                if (match == null)
                {
                    return ($"Element '{pathPrefix}.{elementId}' was not found on the DPP.", null);
                }

                string currentPath = $"{pathPrefix}.{elementId}";

                // Nested DataElementCollection: per EN 18223 Annex A Example 1, children live
                // under the 'elements' array. Recurse into the live OPC UA subtree.
                if (elementObj.TryGetPropertyValue("elements", out JsonNode nestedElements) && nestedElements is JsonArray nestedArray)
                {
                    var (err, nestedWrites) = await ResolveElementWritesAsync(userId, nodesetIdentifier, match, nestedArray, currentPath).ConfigureAwait(false);
                    if (err != null)
                    {
                        return (err, null);
                    }

                    writes.AddRange(nestedWrites);
                    continue;
                }

                if (elementObj.TryGetPropertyValue("value", out JsonNode valueNode))
                {
                    // MultiLanguageDataElement.value is a leaf payload (array of {value,language}
                    // entries, no elementId). Per EN 18223 Annex A, the whole array is the
                    // localized value of this single variable and must not be recursed into.
                    bool isMultiLanguage = elementObj.TryGetPropertyValue("objectType", out JsonNode objectTypeNode)
                        && objectTypeNode is JsonValue objectTypeValue
                        && objectTypeValue.TryGetValue(out string objectType)
                        && string.Equals(objectType, "MultiLanguageDataElement", StringComparison.Ordinal);

                    // MultiValuedDataElement: per EN 18223 Annex A Example 4, the homogenous
                    // child DataElements are nested under 'value' as a JSON array. Recurse so
                    // each child resolves to its own OPC UA node id.
                    if (valueNode is JsonArray valueArray && !isMultiLanguage)
                    {
                        var (err, nestedWrites) = await ResolveElementWritesAsync(userId, nodesetIdentifier, match, valueArray, currentPath).ConfigureAwait(false);
                        if (err != null)
                        {
                            return (err, null);
                        }

                        writes.AddRange(nestedWrites);
                        continue;
                    }

                    // Leaf value update: SingleValuedDataElement.value (any non-array JSON value)
                    // or MultiLanguageDataElement.value (array of {value,language} written as-is).
                    writes.Add((match.Id, JsonNodeToWireValue(valueNode), currentPath));
                    continue;
                }

                // Element entry with no actionable content (e.g. just metadata) is ignored under
                // the merge-patch-shaped semantics described on UpdateDppById: members that are
                // absent from the partial body are left unchanged. (Full RFC 7396 deletion is not
                // supported, because the underlying OPC UA Elements subtree is a fixed schema.)
            }

            return (null, writes);
        }

        // Converts a JSON value to the string payload accepted by UAClient.VariableWrite.
        private static string JsonNodeToWireValue(JsonNode node)
        {
            if (node is null)
            {
                return string.Empty;
            }

            if (node is JsonValue value && value.TryGetValue(out string asString))
            {
                return asString;
            }

            return node.ToJsonString();
        }

        // Snapshots the current variable values from the running OPC UA server back into the
        // DbFiles.Values blob, mirroring BrowserController.Save. Keeping write and persistency
        // separate in UAClient means each update path (DPP, browser UI, future callers) is
        // responsible for explicitly persisting once its in-memory writes succeed.
        //
        // We also advance the nodeset XML's PublicationDate attribute on every save so each
        // persisted snapshot is a distinct version of the nodeset, rather than silently
        // overwriting the previously stored values for the same publication date. This mirrors
        // the in-XML date bump already used by UAClient.CopyNodeset.
        private async Task<bool> PersistNodesetValuesAsync(string userId, string nodesetIdentifier)
        {
            try
            {
                DbFiles file = await _storage.DownloadFileAsync(nodesetIdentifier).ConfigureAwait(false);
                if (file == null)
                {
                    _logger.LogError("PersistNodesetValuesAsync: nodeset {NodesetIdentifier} not found in storage.", nodesetIdentifier);
                    return false;
                }

                Dictionary<string, string> values = await _client.BrowseVariableNodesResursivelyAsync(userId, nodesetIdentifier, null).ConfigureAwait(false);
                if (values == null)
                {
                    _logger.LogError("PersistNodesetValuesAsync: failed to browse variable values for nodeset {NodesetIdentifier}.", nodesetIdentifier);
                    return false;
                }

                string updatedXml = AdvancePublicationDate(file.Blob);
                string serialized = JsonConvert.SerializeObject(values);
                string stored = await _storage.UploadFileAsync(nodesetIdentifier, updatedXml, serialized).ConfigureAwait(false);
                return !string.IsNullOrEmpty(stored);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PersistNodesetValuesAsync failed for nodeset {NodesetIdentifier}.", nodesetIdentifier);
                return false;
            }
        }

        // Rewrites the PublicationDate="..." attribute in the nodeset XML to the current UTC time
        // (second precision). Returns the input unchanged if the attribute is not present or
        // appears malformed; that way callers never lose the XML blob due to a parse mismatch.
        private static string AdvancePublicationDate(string nodesetXml)
        {
            if (string.IsNullOrEmpty(nodesetXml))
            {
                return nodesetXml;
            }

            const string key = "PublicationDate=\"";
            int keyIndex = nodesetXml.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0)
            {
                return nodesetXml;
            }

            int valueStart = keyIndex + key.Length;
            int valueEnd = nodesetXml.IndexOf('"', valueStart);
            if (valueEnd <= valueStart)
            {
                return nodesetXml;
            }

            string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
            var sb = new StringBuilder(nodesetXml);
            sb.Remove(valueStart, valueEnd - valueStart);
            sb.Insert(valueStart, now);
            return sb.ToString();
        }

        // Browses the OPC UA address space to construct a DPP for the given nodeset identifier.
        private async Task<DigitalProductPassport> BrowseDppFromRootAsync(string userId, string nodesetIdentifier)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(userId, nodesetIdentifier, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (nodeList == null)
            {
                return null;
            }

            foreach (NodesetViewerNode node in nodeList)
            {
                if (node.Text == "DigitalProductPassport")
                {
                    return await GenerateDPPFromDPPNode(userId, nodesetIdentifier, node).ConfigureAwait(false);
                }
            }

            return null;
        }

        // Reads DPP properties and child elements from an OPC UA node, building the full DPP object graph.
        private async Task<DigitalProductPassport> GenerateDPPFromDPPNode(string userId, string nodesetIdentifier, NodesetViewerNode dppNode)
        {
            List<NodesetViewerNode> properties = await _client.GetChildren(userId, nodesetIdentifier, dppNode.Id).ConfigureAwait(false);
            if (properties == null)
            {
                return null;
            }

            string uniqueProductId = string.Empty;
            string schemaVersion = string.Empty;
            string status = string.Empty;
            string economicOperator = string.Empty;
            string facilityId = null;
            Granularity granularity = Granularity.Model;
            List<string> contentSpecificationIds = null;
            DateTimeOffset lastUpdate = DateTimeOffset.UtcNow;
            var elements = new List<DataElement>();

            foreach (NodesetViewerNode prop in properties)
            {
                switch (prop.Text)
                {
                    case "UniqueProductIdentifier":
                        uniqueProductId = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "Granularity":
                        string granularityStr = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(granularityStr) &&
                            Enum.TryParse<Granularity>(granularityStr, ignoreCase: true, out Granularity parsedGranularity))
                        {
                            granularity = parsedGranularity;
                        }
                        break;

                    case "DppSchemaVersion":
                        schemaVersion = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "DppStatus":
                        status = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "LastUpdate":
                        string lastUpdateStr = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        if (DateTimeOffset.TryParse(lastUpdateStr, out var parsed))
                        {
                            lastUpdate = parsed;
                        }
                        break;

                    case "EconomicOperatorId":
                        economicOperator = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "FacilityId":
                        facilityId = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "ContentSpecificationIds":
                        string contentSpecRaw = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        contentSpecificationIds = ParseContentSpecificationIds(contentSpecRaw);
                        break;

                    case "Elements":
                        elements.AddRange(await ReadDataElementNodesAsync(userId, nodesetIdentifier, prop).ConfigureAwait(false));
                        break;
                }
            }

            return new DigitalProductPassport {
                DigitalProductPassportId = nodesetIdentifier,
                UniqueProductIdentifier = uniqueProductId,
                Granularity = granularity,
                DppSchemaVersion = schemaVersion,
                DppStatus = status,
                LastUpdate = lastUpdate,
                EconomicOperatorId = economicOperator,
                FacilityId = facilityId,
                ContentSpecificationIds = contentSpecificationIds,
                Elements = elements
            };
        }

        // ContentSpecificationIds is modelled in EN 18223 as a [0..*] list of strings.
        // The underlying OPC UA variable carries it as a single string; accept either a JSON
        // array ("[\"a\",\"b\"]") or a comma-separated value ("a,b") to be tolerant of how
        // a given nodeset author chose to encode the list.
        private static List<string> ParseContentSpecificationIds(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string trimmed = raw.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                try
                {
                    JsonNode parsed = JsonNode.Parse(trimmed);
                    if (parsed is JsonArray arr)
                    {
                        var items = new List<string>(arr.Count);
                        foreach (JsonNode entry in arr)
                        {
                            if (entry is JsonValue v && v.TryGetValue(out string s) && !string.IsNullOrWhiteSpace(s))
                            {
                                items.Add(s);
                            }
                        }
                        return items.Count == 0 ? null : items;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // fall through to comma-separated parsing below
                }
            }

            var split = new List<string>();
            foreach (string part in trimmed.Split(','))
            {
                string s = part.Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    split.Add(s);
                }
            }
            return split.Count == 0 ? null : split;
        }

        // Recursively reads OPC UA child nodes to build a DataElement tree
        private async Task<List<DataElement>> ReadDataElementNodesAsync(string userId, string nodesetIdentifier, NodesetViewerNode parentNode)
        {
            var output = new List<DataElement>();

            List<NodesetViewerNode> childNodes = await _client.GetChildren(userId, nodesetIdentifier, parentNode.Id).ConfigureAwait(false);
            if (childNodes == null)
            {
                return output;
            }

            foreach (NodesetViewerNode childNode in childNodes)
            {
                // Check for children – if present, create a collection; otherwise, a leaf element.
                List<DataElement> grandChildren = await ReadDataElementNodesAsync(userId, nodesetIdentifier, childNode).ConfigureAwait(false);
                if (grandChildren.Count > 0)
                {
                    output.Add(new DataElementCollection {
                        ElementId = childNode.Text,
                        Elements = grandChildren
                    });
                }
                else
                {
                    string value = await _client.VariableRead(userId, nodesetIdentifier, childNode.Id).ConfigureAwait(false);
                    output.Add(new SingleValuedDataElement {
                        ElementId = childNode.Text,
                        Value = JsonValue.Create(value)
                    });
                }
            }

            return output;
        }
    }
}
