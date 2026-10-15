using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly IDppAccessPolicy _accessPolicy;
        private readonly ValueSets.IValueSetStore _valueSets;

        public DPPService(UAClient client, CloudLibDataProvider dataProvider, DbFileStorage storage, IDppVersionArchive archive, IDppAccessPolicy accessPolicy, ValueSets.IValueSetStore valueSets, ILoggerFactory loggerFactory)
        {
            _client = client;
            _dataProvider = dataProvider;
            _storage = storage;
            _archive = archive;
            _accessPolicy = accessPolicy;
            _valueSets = valueSets;
            _logger = loggerFactory.CreateLogger("DPPService");
        }

        /// <summary>
        /// Loads the per-DPP <c>controlledElements</c> mapping (element path -> permitted roles) from the
        /// DPP's stored values blob, together with whether that mapping was absent, valid, or malformed.
        /// Callers must fail closed on <see cref="DppControlledElements.MappingState.Invalid"/>: an
        /// unreadable policy is a failure to determine access, not proof that access is unrestricted.
        /// </summary>
        /// <remarks>
        /// Neither a storage fault nor a missing row is reported as an absent mapping; both become
        /// <see cref="DppControlledElements.MappingState.Invalid"/>. These reads are anonymous, and
        /// "the policy could not be loaded" is indistinguishable in shape from "this DPP controls
        /// nothing" - but only the second one means the data is public. A DPP is materialised from
        /// the live OPC UA address space, so it can still be served while its stored row is missing;
        /// treating that as "no controlled elements" would publish every protected element of a
        /// passport whose policy simply is not there. <see cref="DppControlledElements.MappingState.Absent"/>
        /// is therefore reserved for a row that exists and genuinely declares no policy.
        /// </remarks>
        public async Task<DppControlledElements.MappingResult> GetControlledElementsAsync(string dppId)
        {
            // A passport is a value set, but its access policy lives on the template nodeset it is
            // based on. An id that resolves to no set fails closed exactly like a missing policy row:
            // "could not find the policy" must never read as "nothing is controlled".
            string nodesetIdentifier = Guid.TryParse(dppId, out Guid valueSetId)
                ? await _valueSets.GetNodesetIdentifierAsync(valueSetId).ConfigureAwait(false)
                : null;

            if (string.IsNullOrEmpty(nodesetIdentifier))
            {
                _logger.LogError(
                    "DPP {DppId} does not resolve to a value set, so its template's controlled-elements mapping cannot be located; denying access to all elements.",
                    dppId);

                return DppControlledElements.Unavailable();
            }

            DbFiles file;
            try
            {
                file = await _storage.DownloadFileAsync(nodesetIdentifier).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not load the controlled-elements mapping for DPP {DppId}; denying access to all elements because the policy is unknown, not absent.",
                    dppId);

                return DppControlledElements.Unavailable();
            }

            if (file is null)
            {
                _logger.LogError(
                    "No stored values row exists for DPP {DppId}, so its controlled-elements mapping cannot be read; denying access to all elements rather than treating the missing policy as an absent one.",
                    dppId);

                return DppControlledElements.Unavailable();
            }

            DppControlledElements.MappingResult mapping = DppControlledElements.Read(file.Values);
            if (mapping.IsInvalid)
            {
                _logger.LogError(
                    "Controlled-elements mapping for DPP {DppId} is malformed; denying access to all elements until it is repaired.",
                    dppId);
            }

            return mapping;
        }

        /// <summary>
        /// True when a caller holding <paramref name="callerRoles"/> may read the element addressed by
        /// <paramref name="elementIdPath"/>, per the DPP's own controlled-elements mapping (EN 18239 §5.2).
        /// The access key is the element's path (dotted <c>elementId</c> chain), matching how the API
        /// addresses elements. Public elements are readable by anyone, including anonymous callers.
        /// </summary>
        public async Task<bool> CanReadElementAsync(string dppId, string elementIdPath, IEnumerable<string> callerRoles)
        {
            DppControlledElements.MappingResult mapping = await GetControlledElementsAsync(dppId).ConfigureAwait(false);
            if (mapping.IsInvalid)
            {
                return false;
            }

            if (mapping.Entries.Count == 0)
            {
                return true;
            }

            // Must match how GetElement resolves the path, or the policy is consulted for a key the
            // resolver never uses and a controlled element is served as public.
            string accessKey = BuildCanonicalAccessKey(elementIdPath);
            if (accessKey is null)
            {
                return false;
            }

            return _accessPolicy.CanRead(accessKey, callerRoles, mapping.Entries);
        }
        /// <paramref name="elementIdPath"/>. Write rights mirror read rights: an element controlled for
        /// reading is equally controlled for writing, so a caller that may not see an element may not
        /// change it either. Elements outside the mapping stay writable by any authorized API principal,
        /// preserving the existing contract for public data.
        /// </summary>
        public async Task<bool> CanWriteElementAsync(string dppId, string elementIdPath, IEnumerable<string> callerRoles)
        {
            DppControlledElements.MappingResult mapping = await GetControlledElementsAsync(dppId).ConfigureAwait(false);
            if (mapping.IsInvalid)
            {
                return false;
            }

            if (mapping.Entries.Count == 0)
            {
                return true;
            }

            // Same canonicalization as the write resolver, so a PATCH cannot be authorized against a
            // different key than the one it ultimately modifies.
            string accessKey = BuildCanonicalAccessKey(elementIdPath);
            if (accessKey is null)
            {
                return false;
            }

            return _accessPolicy.CanRead(accessKey, callerRoles, mapping.Entries);
        }

        /// <summary>
        /// True when a caller holding <paramref name="callerRoles"/> may submit a whole-DPP patch.
        /// Because such a patch can address any element, the caller must satisfy every controlled
        /// element's role requirement; otherwise a partially-authorized writer could modify data it
        /// cannot read. DPPs with no controlled elements stay writable by any authorized principal.
        /// </summary>
        public async Task<bool> CanWriteDppAsync(string dppId, IEnumerable<string> callerRoles)
        {
            DppControlledElements.MappingResult mapping = await GetControlledElementsAsync(dppId).ConfigureAwait(false);
            if (mapping.IsInvalid)
            {
                return false;
            }

            if (mapping.Entries.Count == 0)
            {
                return true;
            }

            string[] roles = callerRoles?.ToArray() ?? Array.Empty<string>();
            return mapping.Entries.Keys.All(path => _accessPolicy.CanRead(path, roles, mapping.Entries));
        }

        /// <summary>
        /// Prunes a single element's subtree so that a readable ancestor never leaks controlled
        /// descendants. Returns null when the element itself is not readable.
        /// </summary>
        public async Task<DataElement> FilterElementForRolesAsync(string dppId, string elementIdPath, DataElement element, IEnumerable<string> callerRoles)
        {
            if (element is null)
            {
                return null;
            }

            DppControlledElements.MappingResult mapping = await GetControlledElementsAsync(dppId).ConfigureAwait(false);
            if (mapping.IsInvalid)
            {
                return null;
            }

            if (mapping.Entries.Count == 0)
            {
                return element;
            }

            string[] roles = callerRoles?.ToArray() ?? Array.Empty<string>();

            // The addressed element's own path is the access key for its subtree, canonicalized the
            // same way the resolver canonicalizes it. A null key means the path cannot be expressed
            // as one the mapping could match (unparseable, collection root, or index-addressed), so
            // deny rather than skipping the check - the other call sites treat null the same way.
            string basePath = BuildCanonicalAccessKey(elementIdPath);
            if (basePath is null || !_accessPolicy.CanRead(basePath, roles, mapping.Entries))
            {
                return null;
            }

            string parentPath = TrimLastSegment(basePath);
            List<DataElement> filtered = FilterElements(new List<DataElement> { element }, roles, mapping.Entries, parentPath);
            return filtered.Count == 0 ? null : filtered[0];
        }

        // Drops the final dotted segment so a child's key is built as "<parent>.<elementId>".
        private static string TrimLastSegment(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            int index = path.LastIndexOf('.');
            return index < 0 ? null : path.Substring(0, index);
        }

        /// <summary>
        /// Index of the first segment that actually addresses an element, skipping the optional
        /// leading <c>elements</c> collection name.
        /// </summary>
        /// <remarks>
        /// Resolution (<see cref="GetElement"/>, <c>UpdateDataElement</c>) accepts <c>$.elements.X</c>
        /// and <c>X</c> as the same element. Authorization must therefore consume the prefix the same
        /// way: keying the policy off <c>elements.X</c> while the tree resolves <c>X</c> means the
        /// mapping entry for <c>X</c> is never consulted, and a controlled element is served as if it
        /// were public. Any path normalization used for access control has to go through here.
        /// </remarks>
        internal static int ElementSegmentStart(IReadOnlyList<DppJsonPath.Segment> segments)
        {
            if (segments is null || segments.Count == 0)
            {
                return 0;
            }

            return segments[0].IsName && string.Equals(segments[0].Name, "elements", StringComparison.Ordinal)
                ? 1
                : 0;
        }

        /// <summary>
        /// Builds the canonical access key for a caller-supplied path: the dotted element-id chain as
        /// the resolver sees it, with the optional <c>elements</c> prefix removed.
        /// </summary>
        /// <remarks>
        /// Returns null when the path cannot be parsed, addresses only the collection root, or uses an
        /// index selector &#8212; all of which callers must treat as "deny" rather than "unmapped,
        /// therefore public".
        /// <para>
        /// Index selectors are refused rather than ignored. The mapping is keyed by the dotted chain of
        /// real <c>elementId</c> values, but an index names a position rather than an element, so it
        /// contributes nothing to the key while the resolver still descends through it. Dropping the
        /// index would reduce <c>$.elements[0].abc</c> to <c>abc</c> while the resolver reaches
        /// <c>&lt;rootId&gt;.abc</c>: controls on <c>&lt;rootId&gt;</c> or <c>&lt;rootId&gt;.abc</c>
        /// would never be consulted, and an anonymous read or an authenticated write could reach a
        /// controlled node as if it were public. Resolving the index to its element id instead would
        /// need the DPP tree, which authorization deliberately does not load; refusing keeps this a
        /// pure function and fails closed. Callers can always address the same node by name.
        /// </para>
        /// </remarks>
        internal static string BuildCanonicalAccessKey(string elementIdPath)
        {
            if (string.IsNullOrWhiteSpace(elementIdPath)
                || !DppJsonPath.TryParse(elementIdPath, out IReadOnlyList<DppJsonPath.Segment> segments, out _))
            {
                return null;
            }

            int start = ElementSegmentStart(segments);
            if (start >= segments.Count)
            {
                return null;
            }

            // Any index selector in the addressing portion of the path makes the key unable to
            // describe the node the resolver will reach, so refuse instead of building a key that
            // silently omits it. The skipped "elements" prefix is not examined: it names the
            // collection, not an element.
            for (int i = start; i < segments.Count; i++)
            {
                if (!segments[i].IsName)
                {
                    return null;
                }
            }

            string key = BuildElementPath(segments, start);

            // Defensive: a name-only segment run always produces a non-empty key, but returning ""
            // would be looked up, miss, and read as unmapped - i.e. public - so deny instead.
            return string.IsNullOrEmpty(key) ? null : key;
        }

        // Builds the dotted element-id path used as the access key from parsed JSONPath segments,
        // ignoring array-index segments (access rights apply uniformly to all items of a collection).
        private static string BuildElementPath(IReadOnlyList<DppJsonPath.Segment> segments)
            => BuildElementPath(segments, 0);

        private static string BuildElementPath(IReadOnlyList<DppJsonPath.Segment> segments, int startIndex)
        {
            var builder = new StringBuilder();
            for (int i = startIndex; i < segments.Count; i++)
            {
                DppJsonPath.Segment segment = segments[i];
                if (segment.IsName)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append('.');
                    }

                    builder.Append(segment.Name);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns a copy of <paramref name="dpp"/> with every controlled data element the caller is not
        /// authorized to read removed, so unauthenticated/under-privileged callers only ever see public
        /// elements. The controlled mapping is the DPP's own (carried in its values blob). Controlled
        /// elements are pruned recursively from nested collections. Returns the same instance when
        /// nothing is filtered to avoid needless allocation.
        /// </summary>
        public async Task<DigitalProductPassport> FilterForRolesAsync(DigitalProductPassport dpp, IEnumerable<string> callerRoles)
        {
            if (dpp is null)
            {
                return null;
            }

            DppControlledElements.MappingResult mapping = await GetControlledElementsAsync(dpp.DigitalProductPassportId).ConfigureAwait(false);
            return FilterForRoles(dpp, callerRoles, mapping);
        }

        /// <summary>
        /// As <see cref="FilterForRolesAsync(DigitalProductPassport, IEnumerable{string})"/>, but
        /// applies an explicitly supplied mapping instead of loading the DPP's current one.
        /// </summary>
        /// <remarks>
        /// Historical reads must use this: the stored mapping only describes the DPP as it is now, so
        /// filtering an archived version against it would publish an element that was controlled at
        /// the requested date and has since been un-mapped.
        /// </remarks>
        public DigitalProductPassport FilterForRoles(
            DigitalProductPassport dpp,
            IEnumerable<string> callerRoles,
            DppControlledElements.MappingResult mapping)
        {
            if (dpp is null)
            {
                return null;
            }

            if (mapping is null || mapping.IsInvalid)
            {
                // Fail closed: we cannot tell which elements are controlled, so expose none of them.
                return new DigitalProductPassport {
                    DigitalProductPassportId = dpp.DigitalProductPassportId,
                    UniqueProductIdentifier = dpp.UniqueProductIdentifier,
                    Granularity = dpp.Granularity,
                    DppSchemaVersion = dpp.DppSchemaVersion,
                    DppStatus = dpp.DppStatus,
                    LastUpdate = dpp.LastUpdate,
                    EconomicOperatorId = dpp.EconomicOperatorId,
                    FacilityId = dpp.FacilityId,
                    ContentSpecificationIds = dpp.ContentSpecificationIds,
                    Elements = new List<DataElement>()
                };
            }

            IReadOnlyDictionary<string, string[]> controlled = mapping.Entries;
            if (controlled.Count == 0)
            {
                // No controlled elements for this DPP: everything is public, nothing to filter.
                return dpp;
            }

            string[] roles = callerRoles?.ToArray() ?? Array.Empty<string>();
            List<DataElement> filtered = FilterElements(dpp.Elements, roles, controlled, null);
            if (ReferenceEquals(filtered, dpp.Elements))
            {
                return dpp;
            }

            return new DigitalProductPassport {
                DigitalProductPassportId = dpp.DigitalProductPassportId,
                UniqueProductIdentifier = dpp.UniqueProductIdentifier,
                Granularity = dpp.Granularity,
                DppSchemaVersion = dpp.DppSchemaVersion,
                DppStatus = dpp.DppStatus,
                LastUpdate = dpp.LastUpdate,
                EconomicOperatorId = dpp.EconomicOperatorId,
                FacilityId = dpp.FacilityId,
                ContentSpecificationIds = dpp.ContentSpecificationIds,
                Elements = filtered
            };
        }

        // Recursively drops elements the caller may not read; descends into every element container
        // (see ChildElementsOf), tracking each element's dotted path so access is keyed by element
        // address (not dictionaryReference). Returns the original list reference unchanged when
        // nothing was pruned.
        private List<DataElement> FilterElements(List<DataElement> elements, string[] roles, IReadOnlyDictionary<string, string[]> controlled, string parentPath)
        {
            if (elements is null || elements.Count == 0)
            {
                return elements;
            }

            var result = new List<DataElement>(elements.Count);
            bool changed = false;

            foreach (DataElement element in elements)
            {
                string path = string.IsNullOrEmpty(parentPath) ? element.ElementId : parentPath + "." + element.ElementId;

                if (!_accessPolicy.CanRead(path, roles, controlled))
                {
                    changed = true;
                    continue;
                }

                DataElement pruned = FilterContainer(element, roles, controlled, path);
                if (!ReferenceEquals(pruned, element))
                {
                    changed = true;
                }

                result.Add(pruned);
            }

            return changed ? result : elements;
        }

        /// <summary>
        /// Filters the children of a container element, returning a clone when anything was pruned
        /// and the original instance otherwise. Leaves are returned unchanged.
        /// </summary>
        /// <remarks>
        /// Cloning rather than mutating matters: these instances come from the cached/browsed DPP,
        /// so pruning in place would let one caller's role filtering leak into what the next caller
        /// sees.
        /// </remarks>
        private DataElement FilterContainer(DataElement element, string[] roles, IReadOnlyDictionary<string, string[]> controlled, string path)
        {
            switch (element)
            {
                case DataElementCollection coll:
                {
                    List<DataElement> childFiltered = FilterElements(coll.Elements, roles, controlled, path);
                    return ReferenceEquals(childFiltered, coll.Elements)
                        ? element
                        : new DataElementCollection {
                            ElementId = coll.ElementId,
                            DictionaryReference = coll.DictionaryReference,
                            Elements = childFiltered
                        };
                }

                case MultiValuedDataElement multi:
                {
                    List<DataElement> valueFiltered = FilterElements(multi.Value, roles, controlled, path);
                    return ReferenceEquals(valueFiltered, multi.Value)
                        ? element
                        : new MultiValuedDataElement {
                            ElementId = multi.ElementId,
                            DictionaryReference = multi.DictionaryReference,
                            ValueDataType = multi.ValueDataType,
                            Value = valueFiltered
                        };
                }

                default:
                    return element;
            }
        }

        /// <summary>
        /// True when <paramref name="userId"/> is allowed to see the nodeset backing a DPP, i.e. the
        /// nodeset is published, ownerless, or owned by that user. A null/empty user id means an
        /// anonymous caller and restricts the result to published nodesets.
        /// </summary>
        private async Task<bool> IsNodesetAccessibleAsync(string userId, string nodesetIdentifier)
        {
            if (string.IsNullOrWhiteSpace(nodesetIdentifier))
            {
                return false;
            }

            return await _dataProvider.GetNodeSets(userId, nodesetIdentifier)
                .AnyAsync()
                .ConfigureAwait(false);
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
        /// EN 18222 (Method ReadDPPVersionByIdAndDate), together with the access policy that applied
        /// to that version. If the requested timestamp is at or after the live DPP's own
        /// <see cref="DigitalProductPassport.LastUpdate"/>, the live DPP is returned; otherwise the
        /// archive is consulted for the latest snapshot at or before that timestamp. Returns
        /// <c>null</c> when no version of the DPP existed at the requested point in time.
        /// </summary>
        public async Task<DppVersionSnapshot> GetDppVersionByIdAndDate(string userId, string dppId, DateTimeOffset asOfUtc)
        {
            DateTimeOffset target = asOfUtc.ToUniversalTime();

            // Authorize against the passport before touching either the live DPP or the archive. The
            // archive is a separate store keyed only by dppId: without this check a private or
            // deleted passport would still surrender its historical snapshots to any caller.
            ValueSetPassport passport = await ResolvePassportAsync(userId, dppId).ConfigureAwait(false);
            if (passport is null)
            {
                return null;
            }
            // Resolve the live DPP first: it is the active version for any target at or after its
            // own LastUpdate, which is the common case (most ReadDPPVersionByIdAndDate calls ask
            // for a timestamp that is in the past relative to "now" but still after the most
            // recent update). Consulting the archive first here would return a stale pre-update
            // snapshot for those calls; falling through to the archive only when target predates
            // the live activation matches the EN 18222 contract that the result is the version
            // that was actually active at 'target'.
            DigitalProductPassport live = await BuildPassportAsync(userId, passport).ConfigureAwait(false);

            // No version of a passport exists before its value set was created. BuildPassportAsync
            // already floors LastUpdate at the creation time, but the archive below is searched
            // independently, so the cutoff is enforced here too.
            if (target < passport.CreatedAt.ToUniversalTime())
            {
                return null;
            }

            if (live != null && live.LastUpdate.ToUniversalTime() <= target)
            {
                // The live version's policy is the current stored mapping, by definition - but only
                // when that row actually exists. A DPP is materialised from the live OPC UA address
                // space, so it can still be served while its DbFiles row is missing; passing a null
                // blob through as an archived policy would read as "no controlled elements" and
                // publish the whole element tree. Report the policy as unknown instead, which sends
                // FilterVersionForRoles down its fail-closed path, exactly as an ordinary read
                // treats a missing policy row as unavailable rather than absent.
                DbFiles liveFile = await _storage.DownloadFileAsync(passport.NodesetIdentifier).ConfigureAwait(false);
                return liveFile is null
                    ? new DppVersionSnapshot(live, null, policyArchived: false)
                    : new DppVersionSnapshot(live, liveFile.Values, policyArchived: true);
            }

            // Target is strictly earlier than the live DPP's activation time (or the DPP has no
            // live counterpart at all). Look up the latest archived snapshot whose valid-from
            // timestamp is at or before the target. Keyed by the normalized passport id, exactly as
            // updates archive it, so "ABC..." and "abc..." or an undashed GUID find the same history.
            DppVersionSnapshot archived = await _archive.GetVersionAtAsync(passport.DppId, target).ConfigureAwait(false);
            if (archived != null)
            {
                return archived;
            }

            // No archived snapshot covers the requested timestamp either. This is the genuine
            // "no version existed at that point in time" case (e.g. the target predates the very
            // first archived version), so a 404 from the controller is correct.
            return null;
        }

        /// <summary>
        /// Filters an archived version against the access policy that was in force when it was
        /// captured, rather than the DPP's current mapping.
        /// </summary>
        /// <remarks>
        /// A snapshot may carry no policy for two reasons: it was archived before the archive
        /// recorded policy, or it is the live version and its stored values row is missing. Neither
        /// can be reconstructed after the fact, and substituting today's mapping is precisely the
        /// disclosure this guards against, so both are filtered as if every element were controlled:
        /// the caller receives the DPP's public envelope with no data elements.
        /// </remarks>
        public DigitalProductPassport FilterVersionForRoles(DppVersionSnapshot snapshot, IEnumerable<string> callerRoles)
        {
            if (snapshot?.Dpp is null)
            {
                return null;
            }

            if (!snapshot.PolicyArchived)
            {
                _logger.LogWarning(
                    "No access policy is available for the requested version of DPP {DppId}; withholding all elements rather than applying the current mapping.",
                    snapshot.Dpp.DigitalProductPassportId);

                // Passing a null mapping selects the fail-closed path.
                return FilterForRoles(snapshot.Dpp, callerRoles, mapping: null);
            }

            DppControlledElements.MappingResult mapping = DppControlledElements.Read(snapshot.ControlledElementsValuesJson);
            if (mapping.IsInvalid)
            {
                _logger.LogError(
                    "Archived controlled-elements mapping for DPP {DppId} is malformed; denying access to all elements.",
                    snapshot.Dpp.DigitalProductPassportId);
            }

            return FilterForRoles(snapshot.Dpp, callerRoles, mapping);
        }

        /// <summary>
        /// The readable passport whose value set gives it <paramref name="productId"/> as its unique
        /// product identifier, or null. When several do, the most recently modified set wins.
        /// </summary>
        public async Task<DigitalProductPassport> GetByProductId(string userId, string productId)
        {
            List<ValueSetPassport> matches = await FindPassportsByProductIdsAsync(userId, new[] { productId }).ConfigureAwait(false);
            ValueSetPassport newest = matches.FirstOrDefault();
            if (newest is null)
            {
                _logger.LogWarning("DPP not found for product id {ProductId}", productId);
                return null;
            }

            return await BuildPassportAsync(userId, newest).ConfigureAwait(false);
        }
        /// <summary>
        /// Ids of the readable passports whose unique product identifier is one of
        /// <paramref name="productIds"/>, newest first. Duplicate product identifiers are allowed,
        /// so one product id can map to several passports; all of them are returned.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetDppIdsByProductIds(string userId, IReadOnlyList<string> productIds)
        {
            if (productIds is null || productIds.Count == 0)
            {
                return new List<string>();
            }

            List<ValueSetPassport> matches = await FindPassportsByProductIdsAsync(userId, productIds).ConfigureAwait(false);
            return matches.Select(m => m.DppId).ToList();
        }
        /// <summary>
        /// Every readable passport whose value set sets its template's UniqueProductIdentifier node to
        /// one of <paramref name="productIds"/>, newest first.
        /// </summary>
        /// <remarks>
        /// The store narrows candidates to sets holding a requested value in any entry, in one query.
        /// Each candidate is then confirmed against its template's actual UniqueProductIdentifier
        /// node, so a set that carries the same text in some other field never matches. The previous
        /// lookup compared the product id with a node's NodeId rather than its value, so it could not
        /// find a passport by product id at all.
        /// </remarks>
        private async Task<List<ValueSetPassport>> FindPassportsByProductIdsAsync(string userId, IReadOnlyCollection<string> productIds)
        {
            var requested = new HashSet<string>(productIds.Where(p => !string.IsNullOrEmpty(p)), StringComparer.Ordinal);
            var found = new Dictionary<Guid, (ValueSetPassport Passport, DateTimeOffset LastModified)>();
            if (requested.Count == 0)
            {
                return new List<ValueSetPassport>();
            }

            // Per template: its identifier node, and the value a set inherits when it has no override.
            // Resolved once each; a null node also caches "template not visible to this caller".
            var templates = new Dictionary<string, (string IdNode, string Inherited)>(StringComparer.Ordinal);
            async Task<(string IdNode, string Inherited)> TemplateAsync(string nodesetIdentifier)
            {
                if (!templates.TryGetValue(nodesetIdentifier, out (string IdNode, string Inherited) info))
                {
                    string idNode = await IsNodesetAccessibleAsync(userId, nodesetIdentifier).ConfigureAwait(false)
                        ? await FindPropertyNodeIdAsync(userId, nodesetIdentifier, "UniqueProductIdentifier").ConfigureAwait(false)
                        : null;
                    string inherited = idNode is null ? null : await _client.VariableRead(userId, nodesetIdentifier, idNode).ConfigureAwait(false);
                    info = (idNode, inherited);
                    templates[nodesetIdentifier] = info;
                }

                return info;
            }

            // A set's product identifier is its override if it has one, otherwise the template's value -
            // the same fallback a passport read applies. Matching on the effective value is what finds
            // a set that never overrode the identifier, or had its override removed.
            async Task ConsiderAsync(NodesetValueSet set)
            {
                if (found.ContainsKey(set.Id))
                {
                    return;
                }

                (string idNode, string inherited) = await TemplateAsync(set.NodesetIdentifier).ConfigureAwait(false);
                if (idNode is null)
                {
                    return;
                }

                Dictionary<string, string> overlay = ToOverlay(set.Entries);
                string effective = overlay.TryGetValue(idNode, out string own) ? own : inherited;
                if (effective is not null && requested.Contains(effective))
                {
                    found[set.Id] = (ToPassport(set, overlay), set.LastModified);
                }
            }

            // Candidates that override the identifier with a requested value.
            foreach (NodesetValueSet set in await _valueSets.FindReadableByEntryValuesAsync(requested, userId).ConfigureAwait(false))
            {
                await ConsiderAsync(set).ConfigureAwait(false);
            }

            // Candidates that inherit it: every readable set on a template whose own identifier is a
            // requested value. The template's identifier is read through the OPC UA client, which loads
            // the nodeset together with its uploaded values file, so it is the same effective value a
            // passport read returns. The indexed nodeset XML cannot be used to pick these templates: the
            // values file is applied after indexing, so a template whose XML says "SN-0" but whose values
            // say "SN-1" would be missed. Instead every template with at least one readable set is a
            // candidate - bounded by the caller's readable sets, not by all nodesets.
            var matchingTemplates = templates
                .Where(t => t.Value.Inherited is not null && requested.Contains(t.Value.Inherited))
                .Select(t => t.Key)
                .ToHashSet(StringComparer.Ordinal);
            foreach (string nodesetIdentifier in await _valueSets.ListReadableTemplatesAsync(userId).ConfigureAwait(false))
            {
                (string _, string inherited) = await TemplateAsync(nodesetIdentifier).ConfigureAwait(false);
                if (inherited is not null && requested.Contains(inherited))
                {
                    matchingTemplates.Add(nodesetIdentifier);
                }
            }

            foreach (NodesetValueSet set in await _valueSets.FindReadableByTemplatesAsync(matchingTemplates, userId).ConfigureAwait(false))
            {
                await ConsiderAsync(set).ConfigureAwait(false);
            }

            return found.Values.OrderByDescending(r => r.LastModified).Select(r => r.Passport).ToList();
        }

        // The NodeId of a named property directly under the template's DigitalProductPassport object.
        private async Task<string> FindPropertyNodeIdAsync(string userId, string nodesetIdentifier, string browseName)
        {
            NodesetViewerNode dppNode = await FindDppNodeAsync(userId, nodesetIdentifier).ConfigureAwait(false);
            if (dppNode is null)
            {
                return null;
            }

            List<NodesetViewerNode> properties = await _client.GetChildren(userId, nodesetIdentifier, dppNode.Id).ConfigureAwait(false);
            return properties?.FirstOrDefault(p => p.Text == browseName)?.Id;
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

            // Allow consumers to omit a leading "elements" segment for ergonomics; the OPC UA
            // tree is rooted at that collection in our model. Shared with the access-control path so
            // authorization and resolution can never disagree about which element is addressed.
            int startIndex = ElementSegmentStart(segments);

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

                currentChildren = ChildElementsOf(current);
            }

            return current;
        }

        /// <summary>
        /// The child elements a <see cref="DataElement"/> contains, or <c>null</c> for a leaf.
        /// </summary>
        /// <remarks>
        /// Single definition of what "contains children" means, deliberately shared by path
        /// resolution and by the role filter. These two previously each had their own idea of the
        /// element tree: resolution descended both container types while the filter only descended
        /// <see cref="DataElementCollection"/>, so a controlled element nested under a
        /// <see cref="MultiValuedDataElement"/> was addressable but never filtered - it stayed in the
        /// response and was signed into the ESDC. Any future container type must be added here once,
        /// and both behaviours follow.
        /// </remarks>
        internal static IReadOnlyList<DataElement> ChildElementsOf(DataElement element) =>
            element switch {
                DataElementCollection coll => coll.Elements,
                MultiValuedDataElement multi => multi.Value,
                _ => null
            };

        /// <summary>
        /// Result of an <see cref="UpdateDppById"/> call.
        /// </summary>
        public enum UpdateDppResult
        {
            Success,
            NotFound,
            BadRequest,

            /// <summary>
            /// The write failed and every applied change was rolled back, so the DPP is unchanged.
            /// </summary>
            WriteFailed,

            /// <summary>
            /// The write failed and at least one applied change could not be rolled back, so the DPP
            /// is left partially mutated.
            /// </summary>
            /// <remarks>
            /// Distinct from <see cref="WriteFailed"/> because the two demand different responses: a
            /// clean rollback is an ordinary error, whereas a failed rollback means live data no
            /// longer matches any consistent version and needs operator attention. Reporting both
            /// identically would bury the second in the noise of the first.
            /// </remarks>
            WriteFailedPartiallyApplied,

            /// <summary>
            /// The caller lacks a role required to write a controlled element touched by the update.
            /// Nothing was changed.
            /// </summary>
            Forbidden,

            /// <summary>
            /// The passport changed between being read and being written, so the update was refused
            /// rather than applied on top of a view it never saw. Nothing was changed; re-read and retry.
            /// </summary>
            Conflict
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

            // A passport is a value set: resolve it (which also enforces read access) and browse its
            // template for the node ids each field maps to.
            ValueSetPassport passport = await ResolvePassportAsync(userId, dppId).ConfigureAwait(false);
            NodesetViewerNode dppNode = passport is null ? null : await FindDppNodeAsync(userId, passport.NodesetIdentifier).ConfigureAwait(false);
            if (dppNode == null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            List<NodesetViewerNode> dppProperties = await _client.GetChildren(userId, passport.NodesetIdentifier, dppNode.Id).ConfigureAwait(false);
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

                    var (err, resolved) = await ResolveElementWritesAsync(userId, passport.NodesetIdentifier, elementsNode, elementsArray, "elements").ConfigureAwait(false);
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

                // lastUpdate is server-managed: every successful update advances it to a fresh UTC
                // timestamp so the version-activation boundary stays monotonic and authoritative.
                // Allowing a client to set it would let callers backdate a version or collide with
                // an archived snapshot's valid-from stamp, so reject explicit attempts.
                if (string.Equals(entry.Key, "lastUpdate", StringComparison.Ordinal))
                {
                    return (UpdateDppResult.BadRequest, "lastUpdate is server-managed and cannot be set explicitly; it is advanced automatically on every successful update.", null);
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

            // Snapshot the current version for the archive; a passport that cannot be read is gone.
            DigitalProductPassport preUpdate = await BuildPassportAsync(userId, passport).ConfigureAwait(false);
            if (preUpdate is null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            (UpdateDppResult result, string error) = await CommitValueSetWritesAsync(userId, passport, preUpdate, pendingWrites).ConfigureAwait(false);
            if (result != UpdateDppResult.Success)
            {
                return (result, error, null);
            }
            DigitalProductPassport updated = await GetByDppId(userId, dppId).ConfigureAwait(false);
            return (UpdateDppResult.Success, null, updated);
        }

        /// <summary>
        /// Maps every node under the template's Elements collection to its canonical access key (the
        /// dotted element-id chain), so a write addressed by NodeId can be checked against the same
        /// controlled-elements keys a PATCH is. Header properties are not in the map: they sit outside
        /// Elements and no controlled-elements key can name them.
        /// </summary>
        private async Task<Dictionary<string, string>> MapElementAccessKeysAsync(string userId, string nodesetIdentifier)
        {
            var keys = new Dictionary<string, string>(StringComparer.Ordinal);
            NodesetViewerNode dppNode = await FindDppNodeAsync(userId, nodesetIdentifier).ConfigureAwait(false);
            if (dppNode is null)
            {
                return keys;
            }

            List<NodesetViewerNode> properties = await _client.GetChildren(userId, nodesetIdentifier, dppNode.Id).ConfigureAwait(false);
            NodesetViewerNode elements = properties?.FirstOrDefault(p => p.Text == "Elements");
            if (elements is not null)
            {
                await MapChildrenAsync(userId, nodesetIdentifier, elements.Id, null, keys).ConfigureAwait(false);
            }

            return keys;
        }

        private async Task MapChildrenAsync(string userId, string nodesetIdentifier, string parentId, string parentPath, Dictionary<string, string> keys)
        {
            List<NodesetViewerNode> children = await _client.GetChildren(userId, nodesetIdentifier, parentId).ConfigureAwait(false);
            foreach (NodesetViewerNode child in children ?? new List<NodesetViewerNode>())
            {
                string path = parentPath is null ? BuildElementId(child.Id) : parentPath + "." + BuildElementId(child.Id);
                keys[child.Id] = path;
                await MapChildrenAsync(userId, nodesetIdentifier, child.Id, path, keys).ConfigureAwait(false);
            }
        }

        public enum PublishValueSetResult
        {
            Success,
            NotFound,

            /// <summary>
            /// The set's template has no DigitalProductPassport object, so there is no passport to
            /// publish: the DPP URL would return 404.
            /// </summary>
            NotAPassport
        }

        /// <summary>
        /// Publishes or unpublishes a value set. The single publication path for the REST API and
        /// the browser, so both refuse to publish a set whose template cannot produce a passport.
        /// Unpublishing is always allowed, since it only narrows access.
        /// </summary>
        /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException">The set changed after it was loaded.</exception>
        public async Task<PublishValueSetResult> SetValueSetPublishedAsync(string userId, Guid valueSetId, bool publish)
        {
            Models.NodesetValueSet set = await _valueSets.GetAsync(valueSetId, userId).ConfigureAwait(false);
            if (set is null)
            {
                return PublishValueSetResult.NotFound;
            }

            if (publish && await FindDppNodeAsync(userId, set.NodesetIdentifier).ConfigureAwait(false) is null)
            {
                return PublishValueSetResult.NotAPassport;
            }

            return await _valueSets.SetPublishedAsync(valueSetId, userId, publish).ConfigureAwait(false)
                ? PublishValueSetResult.Success
                : PublishValueSetResult.NotFound;
        }

        /// <summary>
        /// Writes and removes node values on a value set through the same rules as a DPP PATCH.
        /// </summary>
        /// <remarks>
        /// A value set is a passport, so a value edit made through the value-set API or the nodeset
        /// browser is a passport update and must behave like one: only the owner may write; the
        /// caller needs the controlled-element role for every element touched; the previous version
        /// is archived; LastUpdate advances; and a concurrent change is refused rather than
        /// overwritten. Routing every value change through here is what keeps those rules from
        /// depending on which screen or endpoint the change came from.
        /// <para>
        /// <paramref name="writes"/> maps a node id to its new value; <paramref name="removals"/>
        /// lists node ids whose override is dropped so they fall back to the template value.
        /// </para>
        /// </remarks>
        public async Task<(UpdateDppResult Result, string ErrorMessage)> UpdateValueSetValuesAsync(
            string userId,
            Guid valueSetId,
            IReadOnlyDictionary<string, string> writes,
            IReadOnlyCollection<string> removals,
            IEnumerable<string> callerRoles)
        {
            ValueSetPassport passport = await ResolvePassportAsync(userId, valueSetId.ToString()).ConfigureAwait(false);
            if (passport is null)
            {
                return (UpdateDppResult.NotFound, null);
            }

            // A value set on an ordinary nodeset (no DigitalProductPassport object) is a plain overlay,
            // not a passport. It keeps the owner check, per-node validation, revision check and
            // auditing, but has no access policy to enforce and no version history to keep.
            if (await FindDppNodeAsync(userId, passport.NodesetIdentifier).ConfigureAwait(false) is null)
            {
                return await CommitOverlayWritesAsync(userId, passport, writes, removals).ConfigureAwait(false);
            }

            DigitalProductPassport preUpdate = await BuildPassportAsync(userId, passport).ConfigureAwait(false);
            if (preUpdate is null)
            {
                return (UpdateDppResult.NotFound, null);
            }

            var resolved = (writes ?? new Dictionary<string, string>())
                .Select(kv => (NodeId: kv.Key, Value: kv.Value, FieldPath: kv.Key))
                .ToList();

            return await CommitValueSetWritesAsync(userId, passport, preUpdate, resolved, callerRoles ?? Array.Empty<string>(), removals).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes and removes values on a value set whose template is not a Digital Product Passport.
        /// </summary>
        /// <remarks>
        /// An ordinary nodeset's value set is a private overlay, so there is no controlled-elements
        /// policy, version archive or LastUpdate to maintain. It still gets the rules every overlay
        /// edit needs: only the owner may write, each value is validated against its node, and the
        /// revision check refuses a write based on a stale read rather than overwriting a newer one.
        /// The caller audits the outcome.
        /// </remarks>
        private async Task<(UpdateDppResult Result, string ErrorMessage)> CommitOverlayWritesAsync(
            string userId,
            ValueSetPassport set,
            IReadOnlyDictionary<string, string> writes,
            IReadOnlyCollection<string> removals)
        {
            if (!string.Equals(set.OwnerId, userId, StringComparison.Ordinal))
            {
                return (UpdateDppResult.NotFound, null);
            }

            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> kv in writes ?? new Dictionary<string, string>())
            {
                string invalid = await _client.ValidateValueForNode(userId, set.NodesetIdentifier, kv.Key, kv.Value).ConfigureAwait(false);
                if (invalid is not null)
                {
                    return (UpdateDppResult.BadRequest, $"Node '{kv.Key}': {invalid}");
                }

                values[kv.Key] = kv.Value;
            }

            var removed = (removals ?? Array.Empty<string>()).Where(n => !values.ContainsKey(n)).ToList();

            long? committed;
            try
            {
                committed = await _valueSets.ApplyChangesAsync(set.ValueSetId, userId, set.Revision, values, removed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Value set {ValueSetId} could not be saved; nothing was changed.", set.ValueSetId);
                return (UpdateDppResult.WriteFailed, "The change could not be saved.");
            }

            return committed is null
                ? (UpdateDppResult.Conflict, "The value set was changed by another request. Reload it and try again.")
                : (UpdateDppResult.Success, null);
        }
        /// <summary>
        /// Commits resolved writes to a passport's value set, advancing LastUpdate, then archives the
        /// pre-update version.
        /// </summary>
        /// <remarks>
        /// A passport's values live in its value set, so an update is one database transaction rather
        /// than a sequence of writes to the shared OPC UA server, which other passports built on the
        /// same template also read. All writes land or none do, which satisfies EN 18222's "no changes
        /// adopted on failure" outright instead of by best-effort rollback. Each value is checked
        /// against the template node's type and write access first, as a value-set edit is.
        /// <para>
        /// The archive is written after the commit. If archiving fails the previous values are
        /// restored in a second transaction, and only if that also fails is the update reported as
        /// partially applied. Archive keys are the passport id, so each passport keeps its own history.
        /// </para>
        /// </remarks>
        private async Task<(UpdateDppResult Result, string ErrorMessage)> CommitValueSetWritesAsync(
            string userId,
            ValueSetPassport passport,
            DigitalProductPassport preUpdate,
            List<(string NodeId, string Value, string FieldPath)> writes,
            IEnumerable<string> callerRoles = null,
            IReadOnlyCollection<string> removals = null)
        {
            if (!string.Equals(passport.OwnerId, userId, StringComparison.Ordinal))
            {
                // Published sets are readable by anyone but writable only by their owner.
                return (UpdateDppResult.NotFound, null);
            }

            // Read the template's policy source exactly once. That one value is both validated here and
            // archived below, so the policy recorded for the pre-update version can never differ from
            // the policy this write was authorized against. A missing, unreadable or invalid source
            // refuses the update: archiving it would record "no policy", which reads back as public.
            DbFiles policySource;
            try
            {
                policySource = await _storage.DownloadFileAsync(passport.NodesetIdentifier).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update of DPP {DppId} refused: the template's access policy could not be loaded.", passport.DppId);
                return (UpdateDppResult.WriteFailed, "The update was refused because the passport's access policy could not be loaded.");
            }

            DppControlledElements.MappingResult policy = policySource is null
                ? DppControlledElements.Unavailable()
                : DppControlledElements.Read(policySource.Values);
            if (policy.IsInvalid)
            {
                _logger.LogError("Update of DPP {DppId} refused: the template's access policy is missing or invalid.", passport.DppId);
                return (UpdateDppResult.WriteFailed, "The update was refused because the passport's access policy is missing or invalid.");
            }

            // Enforce controlled-element write rights for every element touched, against the policy
            // read above. PATCH callers are also checked by the controller; value-set callers rely on
            // this check alone, so it must hold for every path that changes passport values.
            if (callerRoles is not null && policy.Entries.Count > 0)
            {
                string[] roles = callerRoles.ToArray();
                Dictionary<string, string> accessKeys = await MapElementAccessKeysAsync(userId, passport.NodesetIdentifier).ConfigureAwait(false);
                foreach (string nodeId in writes.Select(w => w.NodeId).Concat(removals ?? Array.Empty<string>()))
                {
                    if (accessKeys.TryGetValue(nodeId, out string key) && !_accessPolicy.CanRead(key, roles, policy.Entries))
                    {
                        return (UpdateDppResult.Forbidden, "Caller is not authorized to modify controlled elements of this DPP.");
                    }
                }
            }

            string lastUpdateNode = await FindPropertyNodeIdAsync(userId, passport.NodesetIdentifier, "LastUpdate").ConfigureAwait(false);
            if (lastUpdateNode is null)
            {
                return (UpdateDppResult.WriteFailed, "The DPP template does not expose a LastUpdate node, so the version timestamp cannot be advanced.");
            }

            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach ((string nodeId, string value, string fieldPath) in writes)
            {
                string invalid = await _client.ValidateValueForNode(userId, passport.NodesetIdentifier, nodeId, value).ConfigureAwait(false);
                if (invalid is not null)
                {
                    return (UpdateDppResult.BadRequest, $"Field '{fieldPath}': {invalid}");
                }

                values[nodeId] = value;
            }

            values[lastUpdateNode] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var removed = new List<string>((removals ?? Array.Empty<string>()).Where(n => !values.ContainsKey(n)));

            // Remember each touched node's previous entry so a failed archive can be undone exactly:
            // nodes the set already overrode get their old value back, the rest are removed again.
            var previous = new Dictionary<string, string>(StringComparer.Ordinal);
            var added = new List<string>();
            foreach (string nodeId in values.Keys.Concat(removed))
            {
                if (passport.Overlay.TryGetValue(nodeId, out string old))
                {
                    previous[nodeId] = old;
                }
                else if (values.ContainsKey(nodeId))
                {
                    added.Add(nodeId);
                }
            }

            // Compare-and-swap against the revision the passport was resolved at: if anything changed
            // the set since, refuse rather than apply this update on top of a view it never saw.
            long? committedRevision;
            try
            {
                committedRevision = await _valueSets.ApplyChangesAsync(passport.ValueSetId, userId, passport.Revision, values, removed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update of DPP {DppId} could not be saved; nothing was changed.", passport.DppId);
                return (UpdateDppResult.WriteFailed, "The update could not be saved.");
            }

            if (committedRevision is null)
            {
                return (UpdateDppResult.Conflict, "The passport was changed by another request. Reload it and try again.");
            }

            if (await _archive.ArchiveAsync(passport.DppId, preUpdate, policySource.Values, preUpdate.LastUpdate.ToUniversalTime()).ConfigureAwait(false))
            {
                return (UpdateDppResult.Success, null);
            }

            _logger.LogError("Update of DPP {DppId}: the previous version could not be archived; restoring the previous values.", passport.DppId);
            try
            {
                // Revision-checked: only undoes this update if nothing has been written since. If a later
                // update has landed, restoring would overwrite it, so it is left in place and reported.
                if (await _valueSets.RestoreValuesAsync(passport.ValueSetId, userId, committedRevision.Value, previous, added).ConfigureAwait(false))
                {
                    return (UpdateDppResult.WriteFailed, "Update could not be completed; previous version could not be archived.");
                }

                _logger.LogError("Update of DPP {DppId}: not restored because a later update has been applied since; this update is applied but unarchived.", passport.DppId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update of DPP {DppId}: restoring the previous values also failed; the update is applied but unarchived.", passport.DppId);
            }

            return (UpdateDppResult.WriteFailedPartiallyApplied, "Update could not be completed; previous version could not be archived.");
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
            // A passport is a value set; resolve it and browse its template, as UpdateDppById does.
            ValueSetPassport passport = await ResolvePassportAsync(userId, dppId).ConfigureAwait(false);
            NodesetViewerNode dppNode = passport is null ? null : await FindDppNodeAsync(userId, passport.NodesetIdentifier).ConfigureAwait(false);
            if (dppNode == null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            List<NodesetViewerNode> dppProperties = await _client.GetChildren(userId, passport.NodesetIdentifier, dppNode.Id).ConfigureAwait(false);
            NodesetViewerNode elementsNode = dppProperties?.FirstOrDefault(p => p.Text == "Elements");
            if (elementsNode == null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            // Allow callers to omit a leading "elements" segment for ergonomics, matching the read path.
            // Shared helper so this can never drift from the key authorization was checked against.
            int startIndex = ElementSegmentStart(segments);

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
            List<NodesetViewerNode> currentChildren = await _client.GetChildren(userId, passport.NodesetIdentifier, current.Id).ConfigureAwait(false);

            for (int i = startIndex; i < endIndex; i++)
            {
                if (currentChildren == null || currentChildren.Count == 0)
                {
                    return (UpdateDppResult.NotFound, null, null);
                }

                DppJsonPath.Segment segment = segments[i];
                NodesetViewerNode next = segment.IsIndex
                    ? (segment.Index.Value >= 0 && segment.Index.Value < currentChildren.Count ? currentChildren[segment.Index.Value] : null)
                    : currentChildren.FirstOrDefault(c => MatchesElementId(c, segment.Name));

                if (next == null)
                {
                    return (UpdateDppResult.NotFound, null, null);
                }

                current = next;

                if (i < endIndex - 1)
                {
                    currentChildren = await _client.GetChildren(userId, passport.NodesetIdentifier, current.Id).ConfigureAwait(false);
                }
                else
                {
                    // Final segment: confirm this is a leaf, not a collection.
                    List<NodesetViewerNode> grandChildren = await _client.GetChildren(userId, passport.NodesetIdentifier, current.Id).ConfigureAwait(false);
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

            // Snapshot the current version for the archive; a passport that cannot be read is gone.
            DigitalProductPassport preUpdate = await BuildPassportAsync(userId, passport).ConfigureAwait(false);
            if (preUpdate is null)
            {
                return (UpdateDppResult.NotFound, null, null);
            }

            var elementWrite = new List<(string NodeId, string Value, string FieldPath)> {
                (current.Id, JsonNodeToWireValue(payloadValue), elementIdPath)
            };

            (UpdateDppResult result, string error) = await CommitValueSetWritesAsync(userId, passport, preUpdate, elementWrite).ConfigureAwait(false);
            if (result != UpdateDppResult.Success)
            {
                return (result, error, null);
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

                NodesetViewerNode match = liveChildren.FirstOrDefault(c => MatchesElementId(c, elementId));
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

        /// <summary>
        /// A value set resolved as a Digital Product Passport: the set supplies the values, and its
        /// template nodeset supplies the structure and the access policy.
        /// </summary>
        /// <remarks>
        /// The nodeset is only a template with default or example values. A passport exists only as
        /// a value set, which is why <see cref="DppId"/> is the set's id rather than the nodeset's.
        /// </remarks>
        private sealed record ValueSetPassport(string DppId, Guid ValueSetId, string NodesetIdentifier, string OwnerId, IReadOnlyDictionary<string, string> Overlay, long Revision, DateTimeOffset CreatedAt);

        private static ValueSetPassport ToPassport(NodesetValueSet set, IReadOnlyDictionary<string, string> overlay) =>
            new(set.Id.ToString(), set.Id, set.NodesetIdentifier, set.UserId, overlay, set.Revision, set.CreatedAt);

        /// <summary>
        /// Resolves <paramref name="dppId"/> to a value set the caller may read, or null.
        /// </summary>
        /// <remarks>
        /// Authorization chokepoint for every DPP read and write. A set is readable by its owner, or
        /// by anyone once published, and its template must also be visible to the caller: the
        /// template is browsed to build the passport, so a set must never become a way to read a
        /// template the caller could not otherwise see. Every failure returns the same null so the
        /// API cannot be used to confirm that a private passport exists.
        /// </remarks>
        private async Task<ValueSetPassport> ResolvePassportAsync(string userId, string dppId)
        {
            if (!Guid.TryParse(dppId, out Guid valueSetId))
            {
                return null;
            }

            NodesetValueSet set = await _valueSets.GetReadableWithValuesAsync(valueSetId, userId).ConfigureAwait(false);
            if (set is null || !await IsNodesetAccessibleAsync(userId, set.NodesetIdentifier).ConfigureAwait(false))
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Denied DPP access to {DppId}: no such value set is visible to the caller.", dppId);
                }

                return null;
            }

            return ToPassport(set, ToOverlay(set.Entries));
        }
        // Keyed by expanded NodeId, the key set entries and NodesetViewerNode.Id share. Ordinal,
        // because NodeIds are case-sensitive: "s=Foo" and "s=foo" are different nodes.
        private static Dictionary<string, string> ToOverlay(IEnumerable<NodesetValueSetEntry> entries)
        {
            var overlay = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (NodesetValueSetEntry entry in entries ?? Enumerable.Empty<NodesetValueSetEntry>())
            {
                if (!string.IsNullOrEmpty(entry?.NodeId))
                {
                    overlay[entry.NodeId] = entry.Value;
                }
            }

            return overlay;
        }

        // The value set's entry for a node when it has one, otherwise the template's value.
        private async Task<string> ReadValueAsync(string userId, string nodesetIdentifier, string nodeId, IReadOnlyDictionary<string, string> overlay)
        {
            if (overlay is not null && nodeId is not null && overlay.TryGetValue(nodeId, out string value))
            {
                return value;
            }

            return await _client.VariableRead(userId, nodesetIdentifier, nodeId).ConfigureAwait(false);
        }

        // Builds a passport from its value set over the template's address space.
        private async Task<DigitalProductPassport> BrowseDppFromRootAsync(string userId, string dppId)
        {
            ValueSetPassport passport = await ResolvePassportAsync(userId, dppId).ConfigureAwait(false);
            return passport is null ? null : await BuildPassportAsync(userId, passport).ConfigureAwait(false);
        }

        private async Task<DigitalProductPassport> BuildPassportAsync(string userId, ValueSetPassport passport)
        {
            NodesetViewerNode dppNode = await FindDppNodeAsync(userId, passport.NodesetIdentifier).ConfigureAwait(false);
            if (dppNode is null)
            {
                return null;
            }

            DigitalProductPassport dpp = await GenerateDPPFromDPPNode(userId, passport.NodesetIdentifier, dppNode, passport.DppId, passport.Overlay).ConfigureAwait(false);
            if (dpp is null)
            {
                return null;
            }

            // A passport's activation time is its own. Until the set records a LastUpdate of its own,
            // the value read above came from the template (or, for a clone, was copied from the
            // source set) and predates this passport, so it is replaced by the set's creation time.
            // Without this a version request for a date before the set existed would return it.
            string lastUpdateNode = await FindPropertyNodeIdAsync(userId, passport.NodesetIdentifier, "LastUpdate").ConfigureAwait(false);
            bool ownLastUpdate = lastUpdateNode is not null
                && passport.Overlay.ContainsKey(lastUpdateNode)
                && dpp.LastUpdate >= passport.CreatedAt;

            return ownLastUpdate ? dpp : WithLastUpdate(dpp, passport.CreatedAt);
        }

        private static DigitalProductPassport WithLastUpdate(DigitalProductPassport dpp, DateTimeOffset lastUpdate) => new() {
            DigitalProductPassportId = dpp.DigitalProductPassportId,
            UniqueProductIdentifier = dpp.UniqueProductIdentifier,
            Granularity = dpp.Granularity,
            DppSchemaVersion = dpp.DppSchemaVersion,
            DppStatus = dpp.DppStatus,
            LastUpdate = lastUpdate,
            EconomicOperatorId = dpp.EconomicOperatorId,
            FacilityId = dpp.FacilityId,
            ContentSpecificationIds = dpp.ContentSpecificationIds,
            Elements = dpp.Elements
        };

        // Locates the template's DigitalProductPassport object under the Objects folder.
        private async Task<NodesetViewerNode> FindDppNodeAsync(string userId, string nodesetIdentifier)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(userId, nodesetIdentifier, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            return nodeList?.FirstOrDefault(n => n.Text == "DigitalProductPassport");
        }
        // Reads DPP properties and child elements from an OPC UA node, building the full DPP object graph.
        private async Task<DigitalProductPassport> GenerateDPPFromDPPNode(string userId, string nodesetIdentifier, NodesetViewerNode dppNode, string dppId, IReadOnlyDictionary<string, string> overlay)
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
                        uniqueProductId = await ReadValueAsync(userId, nodesetIdentifier, prop.Id, overlay).ConfigureAwait(false);
                        break;

                    case "Granularity":
                        string granularityStr = await ReadValueAsync(userId, nodesetIdentifier, prop.Id, overlay).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(granularityStr) &&
                            Enum.TryParse<Granularity>(granularityStr, ignoreCase: true, out Granularity parsedGranularity))
                        {
                            granularity = parsedGranularity;
                        }
                        break;

                    case "DppSchemaVersion":
                        schemaVersion = await ReadValueAsync(userId, nodesetIdentifier, prop.Id, overlay).ConfigureAwait(false);
                        break;

                    case "DppStatus":
                        status = await ReadValueAsync(userId, nodesetIdentifier, prop.Id, overlay).ConfigureAwait(false);
                        break;

                    case "LastUpdate":
                        string lastUpdateStr = await ReadValueAsync(userId, nodesetIdentifier, prop.Id, overlay).ConfigureAwait(false);
                        if (DateTimeOffset.TryParse(lastUpdateStr, out var parsed))
                        {
                            lastUpdate = parsed;
                        }
                        break;

                    case "EconomicOperatorId":
                        economicOperator = await ReadValueAsync(userId, nodesetIdentifier, prop.Id, overlay).ConfigureAwait(false);
                        break;

                    case "FacilityId":
                        facilityId = await ReadValueAsync(userId, nodesetIdentifier, prop.Id, overlay).ConfigureAwait(false);
                        break;

                    case "ContentSpecificationIds":
                        string contentSpecRaw = await ReadValueAsync(userId, nodesetIdentifier, prop.Id, overlay).ConfigureAwait(false);
                        contentSpecificationIds = ParseContentSpecificationIds(contentSpecRaw);
                        break;

                    case "Elements":
                        elements.AddRange(await ReadDataElementNodesAsync(userId, nodesetIdentifier, prop, overlay).ConfigureAwait(false));
                        break;
                }
            }

            return new DigitalProductPassport {
                DigitalProductPassportId = dppId,
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
        private async Task<List<DataElement>> ReadDataElementNodesAsync(string userId, string nodesetIdentifier, NodesetViewerNode parentNode, IReadOnlyDictionary<string, string> overlay)
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
                List<DataElement> grandChildren = await ReadDataElementNodesAsync(userId, nodesetIdentifier, childNode, overlay).ConfigureAwait(false);
                if (grandChildren.Count > 0)
                {
                    output.Add(new DataElementCollection {
                        ElementId = BuildElementId(childNode.Id),
                        Elements = grandChildren
                    });
                }
                else
                {
                    string value = await ReadValueAsync(userId, nodesetIdentifier, childNode.Id, overlay).ConfigureAwait(false);
                    output.Add(new SingleValuedDataElement {
                        ElementId = BuildElementId(childNode.Id),
                        Value = ParseLeafValue(value)
                    });
                }
            }

            return output;
        }

        /// <summary>
        /// True when <paramref name="node"/> is addressed by <paramref name="elementId"/>.
        /// </summary>
        /// <remarks>
        /// Reads emit <see cref="BuildElementId"/> hashes, so writes must match on the same value or
        /// an id obtained from a read could be read back but never updated. The node's raw
        /// <c>Text</c> (its BrowseName) is still accepted so paths written against the earlier
        /// behaviour keep working; it is only a fallback, because BrowseNames can collide across
        /// namespaces whereas the hashed id cannot.
        /// </remarks>
        private static bool MatchesElementId(NodesetViewerNode node, string elementId)
        {
            if (node is null || string.IsNullOrEmpty(elementId))
            {
                return false;
            }

            return string.Equals(BuildElementId(node.Id), elementId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(node.Text, elementId, StringComparison.Ordinal);
        }

        // Produces a stable, globally-unique element id (as a GUID string) for a DPP data element from
        // the node's ExpandedNodeId. The ExpandedNodeId embeds the namespace URI plus the node
        // identifier, so the id is unique even when BrowseNames/DisplayNames collide across namespaces,
        // and is deterministic across reads (unlike a random GUID) so element addressing and the
        // per-DPP access mapping stay stable.
        private static string BuildElementId(string expandedNodeId)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(expandedNodeId ?? string.Empty));
            return new Guid(hash.AsSpan(0, 16)).ToString();
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
