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
        /// EN 18222 (Method ReadDPPVersionByIdAndDate). If the requested timestamp is at or after
        /// the live DPP's own <see cref="DigitalProductPassport.LastUpdate"/>, the live DPP is
        /// returned; otherwise the archive is consulted for the latest snapshot at or before that
        /// timestamp. Returns <c>null</c> when no version of the DPP existed at the requested
        /// point in time.
        /// </summary>
        public async Task<DigitalProductPassport> GetDppVersionByIdAndDate(string userId, string dppId, DateTimeOffset asOfUtc)
        {
            DateTimeOffset target = asOfUtc.ToUniversalTime();

            // Resolve the live DPP first: it is the active version for any target at or after its
            // own LastUpdate, which is the common case (most ReadDPPVersionByIdAndDate calls ask
            // for a timestamp that is in the past relative to "now" but still after the most
            // recent update). Consulting the archive first here would return a stale pre-update
            // snapshot for those calls; falling through to the archive only when target predates
            // the live activation matches the EN 18222 contract that the result is the version
            // that was actually active at 'target'.
            DigitalProductPassport live = await GetByDppId(userId, dppId).ConfigureAwait(false);
            if (live != null && live.LastUpdate.ToUniversalTime() <= target)
            {
                return live;
            }

            // Target is strictly earlier than the live DPP's activation time (or the DPP has no
            // live counterpart at all). Look up the latest archived snapshot whose valid-from
            // timestamp is at or before the target.
            DigitalProductPassport archived = await _archive.GetVersionAtAsync(dppId, target).ConfigureAwait(false);
            if (archived != null)
            {
                return archived;
            }

            // No archived snapshot covers the requested timestamp either. This is the genuine
            // "no version existed at that point in time" case (e.g. the target predates the very
            // first archived version), so a 404 from the controller is correct.
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
            string userId, string dppId, string elementIdPath)
        {
            if (!DppJsonPath.TryParse(elementIdPath, out IReadOnlyList<DppJsonPath.Segment> segments, out string parseError))
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

            // After consuming an optional "elements" prefix the path must still address a specific
            // DataElement. Paths like "elements" or "$.elements" are a client error (they name the
            // collection root, not an element); report this as BadRequest to stay consistent with
            // UpdateDataElement, which returns the same shape for the same semantic mistake.
            if (startIndex >= segments.Count)
            {
                return (ElementResult.BadRequest,
                    "elementIdPath must address a specific element, not the elements root.",
                    null);
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
        /// before issuing any writes, minimizing the risk of partial application. When a write does fail
        /// mid-way, the method makes a best-effort compensating restore of every already-applied write to
        /// its pre-update value (captured immediately before each write); the only residual case where the
        /// live DPP can stay partially mutated is when those restore writes themselves fail, which is logged
        /// for operator reconciliation. A snapshot of the current DPP state is committed to
        /// <see cref="IDppVersionArchive"/> only after the live writes and the persistence step both succeed,
        /// so a failed update never leaves a phantom archive entry. This satisfies the EN 18221 Clause 4.2
        /// archiving requirement (durable archival depends on the configured
        /// <see cref="IDppVersionArchive"/> implementation).
        /// </remarks>
        public async Task<(UpdateDppResult Result, string ErrorMessage, DigitalProductPassport Updated)> UpdateDppById(
            string userId, string dppId, JsonObject partial)
        {
            if (partial is null)
            {
                return (UpdateDppResult.BadRequest, "Request body must contain a partial DPP object.", null);
            }

            // An empty body is a no-op under merge-patch-shaped semantics. Returning early avoids
            // touching the OPC UA address space, creating a redundant archive entry, or bumping
            // the persisted PublicationDate when there is nothing to change.
            if (partial.Count == 0)
            {
                DigitalProductPassport unchanged = await GetByDppId(userId, dppId).ConfigureAwait(false);
                return unchanged is null
                    ? (UpdateDppResult.NotFound, null, null)
                    : (UpdateDppResult.Success, null, unchanged);
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
            // A resolved patch with no concrete writes (e.g. only inert metadata entries) is a
            // no-op: avoid archiving, persisting, or bumping the publication date in that case.
            if (pendingWrites.Count == 0)
            {
                DigitalProductPassport unchanged = await GetByDppId(userId, dppId).ConfigureAwait(false);
                return unchanged is null
                    ? (UpdateDppResult.NotFound, null, null)
                    : (UpdateDppResult.Success, null, unchanged);
            }

            // Snapshot the current DPP before mutating so ReadDPPVersionByIdAndDate can serve the
            // pre-update version (EN 18221 Clause 4.2 archiving requirement). The snapshot is
            // captured here but only committed to the archive after the live writes and the
            // persistence step both succeed, so a failed update never leaves a phantom archive row.
            DigitalProductPassport preUpdate = await GetByDppId(userId, dppId).ConfigureAwait(false);

            // EN 18222 requires that "if the update of some parts fails the complete update process
            // will fail and there should be no changes adopted in the DPP". OPC UA writes are not
            // transactional, so we honor that contract on a best-effort basis: capture each target's
            // current value immediately before writing it, and on the first write failure attempt to
            // restore every already-applied write to its captured baseline. The only residual case
            // where the live DPP can stay partially mutated is when the compensating restore writes
            // themselves fail, which is logged so operators can reconcile manually.
            var appliedWrites = new List<(string NodeId, string OriginalValue, string FieldPath)>();

            foreach (var (nodeId, value, fieldPath) in pendingWrites)
            {
                string originalValue = await _client.VariableRead(userId, dppId, nodeId).ConfigureAwait(false);
                bool ok = await _client.VariableWrite(userId, dppId, nodeId, value).ConfigureAwait(false);
                if (!ok)
                {
                    _logger.LogError("UpdateDppById: write failed for DPP {DppId}, field '{Field}'.", dppId, fieldPath);
                    await TryRollbackWritesAsync(userId, dppId, appliedWrites).ConfigureAwait(false);
                    return (UpdateDppResult.WriteFailed, $"Failed to write field '{fieldPath}'.", null);
                }

                appliedWrites.Add((nodeId, originalValue, fieldPath));
            }

            // Mirror BrowserController.Save: writes go to the live SimpleServer in-memory, so we
            // must explicitly snapshot the variable values back into DbFiles.Values for the change
            // to survive a session / server restart.
            if (!await PersistNodesetValuesAsync(userId, dppId).ConfigureAwait(false))
            {
                _logger.LogError("UpdateDppById: failed to persist updated values for DPP {DppId}.", dppId);
                // The OPC UA writes succeeded but did not make it to durable storage. Honor the
                // best-effort "no changes adopted on failure" contract by reverting the live nodes
                // back to their captured pre-update values; otherwise the in-memory DPP would
                // diverge from what is on disk until the next server restart.
                await TryRollbackWritesAsync(userId, dppId, appliedWrites).ConfigureAwait(false);
                return (UpdateDppResult.WriteFailed, "Update applied in memory but could not be persisted to storage.", null);
            }

            // Only now that the change is durable do we commit the pre-update snapshot to the
            // archive. Archiving last keeps the version history in sync with what actually got
            // persisted and avoids phantom archive entries for failed updates. EN 18221 Clause 4.2
            // makes archival part of the update contract, so if archiving fails we treat the entire
            // update as failed and roll back the already-applied writes to honor the "no changes
            // adopted on failure" contract. This prevents clients from seeing a 500 for a durable
            // update and inadvertently retrying (which would apply the update twice).
            //
            // The archive is keyed by "valid-from" timestamps: a snapshot stored at time T is
            // considered the active version for any query asOfUtc >= T until the next snapshot.
            // The pre-update version became active when its own LastUpdate stamp was written, so
            // we archive it with preUpdate.LastUpdate (UTC-normalized) rather than UtcNow. Using
            // UtcNow would record the moment the *new* version takes over, which makes the
            // ordered lookup in IDppVersionArchive.GetVersionAtAsync miss the snapshot for any
            // asOfUtc < UtcNow even though that asOfUtc was inside the snapshot's validity
            // window.
            if (preUpdate != null)
            {
                bool archived = await _archive.ArchiveAsync(dppId, preUpdate, preUpdate.LastUpdate.ToUniversalTime()).ConfigureAwait(false);
                if (!archived)
                {
                    _logger.LogError("UpdateDppById: archive write failed for DPP {DppId}; rolling back durable update to honor no-changes-on-failure contract.", dppId);
                    await TryRollbackWritesAsync(userId, dppId, appliedWrites).ConfigureAwait(false);
                    if (!await PersistNodesetValuesAsync(userId, dppId).ConfigureAwait(false))
                    {
                        _logger.LogError("UpdateDppById: rollback persist also failed for DPP {DppId}; update is now durable but archive is missing and client sees failure.", dppId);
                    }
                    return (UpdateDppResult.WriteFailed, "Update could not be completed; previous version could not be archived.", null);
                }
            }
            else
            {
                // If preUpdate is null (e.g., GetByDppId failed), we cannot archive a previous version.
                // This violates EN 18221 Clause 4.2, so roll back and fail the update rather than
                // silently skipping archival.
                _logger.LogError("UpdateDppById: could not capture pre-update snapshot for DPP {DppId}; rolling back durable update.", dppId);
                await TryRollbackWritesAsync(userId, dppId, appliedWrites).ConfigureAwait(false);
                await PersistNodesetValuesAsync(userId, dppId).ConfigureAwait(false);
                return (UpdateDppResult.WriteFailed, "Update could not be completed; previous version could not be captured for archival.", null);
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
        /// entire subtree shape. A snapshot of the pre-update DPP is committed to
        /// <see cref="IDppVersionArchive"/> only after the live write and the persistence step both succeed,
        /// matching the archival contract used by <see cref="UpdateDppById"/> and ensuring failed writes
        /// never leave a phantom archive entry. The leaf's pre-update value is captured before the live
        /// write so the method can make a best-effort compensating restore if the persistence step fails,
        /// honoring the same "no changes adopted on failure" contract as <see cref="UpdateDppById"/>.
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

            // A trailing ".value" segment addresses the leaf's value property, not a child node. In our
            // OPC UA model a leaf DataElement has no "value" child (its value is read/written on the
            // element node itself), so walking that segment would fail with NotFound. RFC 9535 callers
            // may legitimately address the leaf via "$.elements.myElement.value", so treat a trailing
            // ".value" as addressing the owning element: drop it before the walk and re-read. We only
            // strip it when at least one element-identifying segment remains after the optional
            // "elements" prefix, otherwise "$.elements.value" would resolve to nothing.
            int endIndex = segments.Count;
            if (segments[endIndex - 1].IsName
                && string.Equals(segments[endIndex - 1].Name, "value", StringComparison.Ordinal)
                && endIndex - 1 > startIndex)
            {
                endIndex--;
            }

            if (startIndex >= endIndex)
            {
                return (UpdateDppResult.BadRequest, "elementIdPath must address a specific element, not the elements root.", null);
            }

            // Walk the live OPC UA tree per JSONPath segment.
            NodesetViewerNode current = elementsNode;
            List<NodesetViewerNode> currentChildren = await _client.GetChildren(userId, dppId, current.Id).ConfigureAwait(false);

            for (int i = startIndex; i < endIndex; i++)
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

                if (i < endIndex - 1)
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

            // Explicit JSON null is not a supported value: the OPC UA Elements subtree is a fixed
            // schema, so null cannot be expressed as a deletion. Reject up front rather than
            // writing an empty string (which is what JsonNodeToWireValue would otherwise produce).
            if (payloadValue is null)
            {
                return (UpdateDppResult.BadRequest,
                    "value must not be null; explicit null is not a supported update for a leaf element.",
                    null);
            }

            // Snapshot the pre-update DPP up front so we capture the version that was live before
            // this write was issued. The snapshot is only committed to the archive once both the
            // VariableWrite and PersistNodesetValuesAsync calls below succeed - that way a failed
            // update never produces a phantom archive entry.
            DigitalProductPassport preUpdate = await GetByDppId(userId, dppId).ConfigureAwait(false);

            // Capture the live leaf value immediately before the write so we can roll back to it
            // if any of the subsequent steps fail. Mirrors the appliedWrites bookkeeping in
            // UpdateDppById and keeps the best-effort "no changes adopted on failure" contract
            // honest for the single-element path too. Failures of the compensating restore itself
            // are logged but otherwise unobservable - same caveat as UpdateDppById.
            string originalLeafValue = await _client.VariableRead(userId, dppId, current.Id).ConfigureAwait(false);

            bool ok = await _client.VariableWrite(userId, dppId, current.Id, JsonNodeToWireValue(payloadValue)).ConfigureAwait(false);
            if (!ok)
            {
                _logger.LogError("UpdateDataElement: write failed for DPP {DppId}, path '{Path}'.", dppId, elementIdPath);
                return (UpdateDppResult.WriteFailed, $"Failed to write element '{elementIdPath}'.", null);
            }

            var appliedWrites = new List<(string NodeId, string OriginalValue, string FieldPath)> {
                (current.Id, originalLeafValue, elementIdPath)
            };

            // Persist values to DbFiles.Values so the write survives a session restart.
            if (!await PersistNodesetValuesAsync(userId, dppId).ConfigureAwait(false))
            {
                _logger.LogError("UpdateDataElement: failed to persist updated values for DPP {DppId}.", dppId);
                await TryRollbackWritesAsync(userId, dppId, appliedWrites).ConfigureAwait(false);
                return (UpdateDppResult.WriteFailed, "Update applied in memory but could not be persisted to storage.", null);
            }

            // Only commit the archive snapshot after the change is durable, matching the ordering
            // used by UpdateDppById and keeping the version history aligned with what actually got
            // persisted (Clause 4.7 - previous version remains addressable, but only for real updates).
            // EN 18221 Clause 4.2 treats archival as part of the update contract, so if archiving
            // fails we roll back the already-applied writes and re-persist to honor the
            // "no changes adopted on failure" contract (same strategy as UpdateDppById). This
            // prevents clients from seeing a 500 for a durable update and inadvertently retrying.
            // The snapshot is stored under its own LastUpdate stamp (i.e. when that version became
            // active) so the archive's at-or-before lookup returns it for any asOfUtc inside its
            // validity window - see the matching comment block in UpdateDppById for the full
            // rationale.
            if (preUpdate != null)
            {
                bool archived = await _archive.ArchiveAsync(dppId, preUpdate, preUpdate.LastUpdate.ToUniversalTime()).ConfigureAwait(false);
                if (!archived)
                {
                    _logger.LogError("UpdateDataElement: archive write failed for DPP {DppId}; rolling back durable update to honor no-changes-on-failure contract.", dppId);
                    await TryRollbackWritesAsync(userId, dppId, appliedWrites).ConfigureAwait(false);
                    if (!await PersistNodesetValuesAsync(userId, dppId).ConfigureAwait(false))
                    {
                        _logger.LogError("UpdateDataElement: rollback persist also failed for DPP {DppId}; update is now durable but archive is missing and client sees failure.", dppId);
                    }
                    return (UpdateDppResult.WriteFailed, "Update could not be completed; previous version could not be archived.", null);
                }
            }
            else
            {
                // If preUpdate is null (e.g., GetByDppId failed), we cannot archive a previous version.
                // This violates EN 18221 Clause 4.2, so roll back and fail the update rather than
                // silently skipping archival.
                _logger.LogError("UpdateDataElement: could not capture pre-update snapshot for DPP {DppId}; rolling back durable update.", dppId);
                await TryRollbackWritesAsync(userId, dppId, appliedWrites).ConfigureAwait(false);
                await PersistNodesetValuesAsync(userId, dppId).ConfigureAwait(false);
                return (UpdateDppResult.WriteFailed, "Update could not be completed; previous version could not be captured for archival.", null);
            }

            // Re-read the updated element to return it in the response. We rebuild the lookup path
            // from the normalized segment range [startIndex, endIndex) so that any trailing ".value"
            // (already excluded above) is not passed to GetElement: ResolveElement matches ElementId
            // values in the DataElement tree and has no terminal "value" node to resolve. The leading
            // "elements" prefix is intentionally omitted because GetElement re-applies the same
            // optional-prefix handling internally.
            string verificationPath = BuildElementPath(segments, startIndex, endIndex);

            (ElementResult readResult, _, DataElement updated) = await GetElement(userId, dppId, verificationPath).ConfigureAwait(false);
            if (readResult != ElementResult.Success)
            {
                return (UpdateDppResult.WriteFailed, "Write succeeded but updated element could not be re-read.", null);
            }

            return (UpdateDppResult.Success, null, updated);
        }

        // Reconstructs a JSONPath string from the normalized segment range [start, end). Name segments
        // render as bracket selectors ('["name"]') so element ids containing dots or other delimiters
        // round-trip unambiguously; index segments render as '[index]'. The result is rooted at '$'.
        private static string BuildElementPath(IReadOnlyList<DppJsonPath.Segment> segments, int start, int end)
        {
            var builder = new System.Text.StringBuilder("$");
            for (int i = start; i < end; i++)
            {
                DppJsonPath.Segment segment = segments[i];
                if (segment.IsIndex)
                {
                    builder.Append('[').Append(segment.Index.Value).Append(']');
                }
                else
                {
                    builder.Append("[\"").Append(segment.Name).Append("\"]");
                }
            }

            return builder.ToString();
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

                // Validate that elementId is a non-empty JSON string. JsonNode.GetValue<string>()
                // throws InvalidOperationException for non-string JSON values (numbers, booleans,
                // objects, arrays), which would surface as a 500 instead of the client-error
                // BadRequest the caller expects for malformed input.
                if (elementIdNode is not JsonValue elementIdValue
                    || !elementIdValue.TryGetValue(out string elementId)
                    || string.IsNullOrEmpty(elementId))
                {
                    return ($"Each entry under '{pathPrefix}' must carry a non-empty string 'elementId'.", null);
                }

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
                    // Explicit JSON null is not a supported update target: the underlying OPC UA
                    // Elements subtree is a fixed schema, so null cannot express a deletion.
                    // Without this guard, JsonNodeToWireValue(null) would silently write "".
                    if (valueNode is null)
                    {
                        return ($"Element '{currentPath}' has an explicit null value; null is not a supported update.", null);
                    }

                    // When 'value' is a JSON array we must distinguish two cases that share the
                    // same wire shape:
                    //   - MultiValuedDataElement (EN 18223 Annex A Example 4): the homogenous
                    //     child DataElements live under 'value' and each child needs to resolve to
                    //     its own OPC UA node id, so we recurse.
                    //   - MultiLanguageDataElement (EN 18223 Annex A): the whole [{value,language}]
                    //     array is the localized value of a single leaf variable and must be
                    //     written to 'match' as-is without recursion.
                    //
                    // The authoritative signal is the live OPC UA tree: collection elements have
                    // child variables under them, leaf variables do not. We probe the server and
                    // only fall back to the client-supplied objectType hint if the server result is
                    // ambiguous (no children at all). This stops a malicious or buggy client from
                    // forcing the server to write an entire array payload onto the wrong parent
                    // node by lying about objectType.
                    if (valueNode is JsonArray valueArray)
                    {
                        List<NodesetViewerNode> matchChildren = await _client.GetChildren(userId, nodesetIdentifier, match.Id).ConfigureAwait(false);
                        bool isCollectionNode = matchChildren != null && matchChildren.Count > 0;

                        if (isCollectionNode)
                        {
                            var (err, nestedWrites) = await ResolveElementWritesAsync(userId, nodesetIdentifier, match, valueArray, currentPath).ConfigureAwait(false);
                            if (err != null)
                            {
                                return (err, null);
                            }

                            writes.AddRange(nestedWrites);
                            continue;
                        }

                        // Leaf node carrying an array payload (e.g. MultiLanguageDataElement).
                        // Fall through to the scalar leaf write below.
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

        // Best-effort compensating rollback for the non-transactional OPC UA write loop in
        // UpdateDppById. The list is iterated in reverse so the most-recently mutated node is
        // restored first, mirroring how callers would unwind a try/finally chain. Failures to
        // restore an individual node are logged but do not throw, because the only useful action
        // from here is to surface as much diagnostic context as possible: at this point the
        // original UpdateDppById call is already returning WriteFailed and the live DPP may
        // still hold some of the in-flight values - logging lets operators reconcile manually.
        private async Task TryRollbackWritesAsync(
            string userId,
            string dppId,
            List<(string NodeId, string OriginalValue, string FieldPath)> appliedWrites)
        {
            for (int i = appliedWrites.Count - 1; i >= 0; i--)
            {
                var (nodeId, originalValue, fieldPath) = appliedWrites[i];
                try
                {
                    bool restored = await _client.VariableWrite(userId, dppId, nodeId, originalValue ?? string.Empty).ConfigureAwait(false);
                    if (!restored)
                    {
                        _logger.LogError(
                            "UpdateDppById: rollback failed for DPP {DppId}, field '{Field}'. Live value may be inconsistent with the original snapshot.",
                            dppId,
                            fieldPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "UpdateDppById: rollback threw for DPP {DppId}, field '{Field}'. Live value may be inconsistent with the original snapshot.",
                        dppId,
                        fieldPath);
                }
            }
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
        // with millisecond precision. Millisecond precision (not second) is required because
        // PersistNodesetValuesAsync is invoked once per update and the README/docs guarantee that
        // every persisted save is a distinct version: two updates landing in the same wall-clock
        // second would otherwise share the same PublicationDate value and silently overwrite each
        // other's "version" stamp. The standard nodeset XML grammar allows a fractional-seconds
        // component on xs:dateTime, so consumers that ignore the fraction still see a valid
        // ISO 8601 / UA-compliant timestamp. Returns the input unchanged if the attribute is not
        // present or appears malformed; that way callers never lose the XML blob due to a parse
        // mismatch.
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

            string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
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
                        Value = ParseLeafValue(value)
                    });
                }
            }

            return output;
        }

        // Leaf variables are persisted as strings by the OPC UA layer, but writes accept typed JSON
        // (numbers, booleans, arrays, objects) via JsonNodeToWireValue, which serializes those as
        // their JSON text form. To round-trip structured writes (objects / arrays) back to clients
        // as typed JSON rather than quoted strings, we attempt to parse the stored string as a
        // JSON literal first - but only when its leading character unambiguously marks structural
        // JSON ('{', '[' or '"'). Bare numeric / boolean / null literals are intentionally NOT
        // re-typed: stored strings such as serial numbers, product IDs or values with significant
        // leading zeros ("007") would otherwise be silently coerced into a JSON number (7), which
        // changes semantics and loses formatting. Clients that need typed scalars can opt in by
        // writing an explicit JSON object/array shape (e.g. a wrapped MultiValuedDataElement) or
        // by parsing the returned string themselves.
        //
        // Exposed as internal (not private) so the unit-test assembly can exercise the round-trip
        // matrix without instantiating the full DPPService dependency graph.
        internal static JsonNode ParseLeafValue(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return JsonValue.Create(raw);
            }

            char first = raw[0];
            // Only attempt parsing when the leading character is a JSON STRUCTURAL start
            // ('{' object, '[' array, '"' quoted string). Numeric / boolean / null literals are
            // intentionally excluded - see method remarks.
            if (first is not ('{' or '[' or '"'))
            {
                return JsonValue.Create(raw);
            }

            try
            {
                JsonNode parsed = JsonNode.Parse(raw);
                return parsed ?? JsonValue.Create(raw);
            }
            catch (System.Text.Json.JsonException)
            {
                return JsonValue.Create(raw);
            }
        }
    }
}
