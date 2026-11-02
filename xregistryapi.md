# xRegistry API for OPC UA NodeSets

The UA Cloud Library exposes its OPC UA information models (NodeSet2 files) through an
[xRegistry](https://xregistry.io) compatible REST API, in addition to the existing `/infomodel`
REST and GraphQL interfaces. xRegistry is a CNCF specification for metadata registries with a
uniform *Registry → Group → Resource → Version* model, so generic xRegistry tooling can discover,
download and (with permission) publish nodesets without knowing anything about the Cloud Library.

The implementation targets the **xRegistry core specification 1.0-rc2**. Deviations from the
specification are listed at the end of this document.

---

## 1. Mapping of the Cloud Library onto the xRegistry model

| xRegistry concept | Cloud Library concept | Id |
|---|---|---|
| Registry | The UA Cloud Library instance | `registryid` = `ua-cloud-library` |
| Group (`nodesetgroups` / `nodesetgroup`) | An OPC UA **namespace URI** (one information model family) | The namespace URI, encoded (see §2) |
| Resource (`nodesets` / `nodeset`) | The NodeSet2 document family of that namespace | Always the constant `nodeset` – every group has exactly one resource |
| Version | One stored NodeSet2 file (a revision of the model) | The Cloud Library **nodeset identifier** (the number used by `/infomodel/download/{identifier}`) |
| Default version | The version with the **latest publication date** | – |
| Resource document | The NodeSet2 XML (`application/xml`) | – |

Example: the three published revisions of *OPC UA for Devices* (`http://opcfoundation.org/UA/DI/`)
appear as one group with one resource `nodeset` that has three versions.

### 1.1 Attributes

Every version (and therefore the resource, which mirrors its default version) carries the standard
xRegistry attributes (`nodesetid`, `versionid`, `self`, `xid`, `epoch`, `name`, `isdefault`,
`description`, `documentation`, `labels`, `createdat`, `modifiedat`, `ancestor`, `contenttype`,
`nodeseturl`/`nodeset`) plus these Cloud Library extension attributes:

| Attribute | Type | Source |
|---|---|---|
| `namespaceuri` | url | Nodeset namespace URI (also on the group) |
| `version` | string | Informational version string from the NodeSet2 `<Model>` |
| `publicationdate` | timestamp | `<Model PublicationDate>` |
| `lastmodifieddate` | timestamp | `<UANodeSet LastModified>` |
| `license`, `licenseurl`, `copyright` | string / url / string | Metadata |
| `iconurl`, `purchasinginformationurl`, `releasenotesurl`, `testspecificationurl` | url | Metadata |
| `keywords`, `supportedlocales` | array of string | Metadata (keywords are also exposed as `labels`) |
| `requiredmodels` | array of `{namespaceuri, publicationdate, version, availableversionid}` | Model dependencies declared in the NodeSet2 |
| `numberofdownloads` | uinteger | Download counter |

The full model is served at `GET /xregistry/model`.

### 1.2 Derived values

The database has no epoch counters or per-entity timestamps, so these are **derived**:

* A version's `epoch` is always `1` (a stored nodeset is immutable; an overwrite replaces it).
* A resource's, meta's and group's `epoch` is the number of versions it holds; the registry's `epoch`
  is the total number of nodesets.
* `createdat`/`modifiedat` of a version are its upload time (falling back to the publication date);
  group/resource/registry use the min/max over their versions.
* `ancestor` chains versions by publication date; the oldest version is its own ancestor.

---

## 2. Group ids (namespace URI encoding)

xRegistry ids are limited to `^[a-zA-Z0-9_][a-zA-Z0-9_.\-~@]{0,127}$`, so a namespace URI cannot be
used verbatim. The group id is the namespace URI with every byte outside `A-Z a-z 0-9 _ . - @`
(and the `~` itself) replaced by `~XX`, the upper-case hex of its UTF-8 byte. The encoding is
reversible, so clients can compute a group id without a lookup:

| Namespace URI | Group id |
|---|---|
| `http://opcfoundation.org/UA/DI/` | `http~3A~2F~2Fopcfoundation.org~2FUA~2FDI~2F` |
| `http://opcfoundation.org/UA/Machinery/` | `http~3A~2F~2Fopcfoundation.org~2FUA~2FMachinery~2F` |

The decoded namespace URI is always available as the `namespaceuri` attribute, so clients can also
simply list `/xregistry/nodesetgroups` and read it from there.

---

## 3. Endpoints

All routes are relative to the registry root `https://<host>/xregistry`.

### 3.1 Read

| Method & path | Returns |
|---|---|
| `GET /xregistry` | Registry root entity |
| `GET /xregistry/model` | Registry model |
| `GET /xregistry/capabilities` | Registry capabilities |
| `GET /xregistry/export` | Root with model, capabilities and all groups/resources/versions inlined |
| `GET /xregistry/nodesetgroups` | Map of all groups, keyed by group id |
| `GET /xregistry/nodesetgroups/{gid}` | One group |
| `GET /xregistry/nodesetgroups/{gid}/nodesets` | Map with the single `nodeset` resource |
| `GET /xregistry/nodesetgroups/{gid}/nodesets/nodeset` | **NodeSet2 XML** of the default version, metadata in `xRegistry-*` headers |
| `GET /xregistry/nodesetgroups/{gid}/nodesets/nodeset$details` | JSON metadata of the resource (default version) |
| `GET /xregistry/nodesetgroups/{gid}/nodesets/nodeset/meta` | Resource meta entity (`defaultversionid`, `defaultversionurl`, …) |
| `GET /xregistry/nodesetgroups/{gid}/nodesets/nodeset/versions` | Map of all versions, keyed by nodeset identifier |
| `GET /xregistry/nodesetgroups/{gid}/nodesets/nodeset/versions/{vid}` | **NodeSet2 XML** of that version, metadata in `xRegistry-*` headers |
| `GET /xregistry/nodesetgroups/{gid}/nodesets/nodeset/versions/{vid}$details` | JSON metadata of that version |

Document responses (`…/nodeset` and `…/versions/{vid}` without `$details`) increment the nodeset's
download counter, exactly like `/infomodel/download`. `$details` responses do not.

#### `?inline` flag

`?inline` (or `?inline=*`) inlines all nested collections; `?inline=<path>[,<path>]` inlines specific
collections. Paths are relative to the addressed entity:

| On | Valid paths |
|---|---|
| `/xregistry` | `model`, `capabilities`, `nodesetgroups`, `nodesetgroups.nodesets`, `nodesetgroups.nodesets.meta`, `nodesetgroups.nodesets.versions`, `nodesetgroups.nodesets.nodeset`, `nodesetgroups.nodesets.versions.nodeset` |
| group | `nodesets`, `nodesets.meta`, `nodesets.versions`, `nodesets.nodeset`, `nodesets.versions.nodeset` |
| resource / version | `meta`, `versions`, `nodeset`, `versions.nodeset` |

Inlining `nodeset` embeds the NodeSet2 XML as a string in the `nodeset` attribute (and omits
`nodeseturl`). `inline=*` deliberately does **not** include the documents, the model or the
capabilities; ask for them explicitly.

### 3.2 Write

| Method & path | Effect |
|---|---|
| `PUT /xregistry/nodesetgroups/{gid}/nodesets/nodeset` | Upload a new version of the namespace |
| `POST /xregistry/nodesetgroups/{gid}/nodesets/nodeset/versions` | Same as the `PUT` above |
| `PUT …/nodesets/nodeset$details` | Upload with a JSON entity body instead of raw XML |
| `DELETE …/nodesets/nodeset/versions/{vid}` | Delete one nodeset (owner of an unpublished nodeset, or administrator) |
| `DELETE …/nodesets/nodeset` | Delete every version of the namespace (**administrator only**) |
| `DELETE /xregistry/nodesetgroups/{gid}` | Same as deleting the resource (**administrator only**) |
| `PUT /xregistry/nodesetgroups/{gid}` | `405` – groups are created implicitly by uploading a nodeset |

**Upload body**

* *Document form* (`…/nodeset`): the body is the NodeSet2 XML. Metadata is passed as
  `xRegistry-<attribute>` request headers: `xRegistry-name`, `xRegistry-description`,
  `xRegistry-license`, `xRegistry-copyright`, `xRegistry-documentation`, `xRegistry-licenseurl`,
  `xRegistry-iconurl`, `xRegistry-purchasinginformationurl`, `xRegistry-releasenotesurl`,
  `xRegistry-testspecificationurl`, `xRegistry-keywords`, `xRegistry-supportedlocales`
  (arrays as a JSON array or a comma-separated list).
* *Entity form* (`…/nodeset$details`): the body is a JSON object with the same attribute names and
  the XML in the `nodeset` string attribute.

The version id, namespace URI, version string and publication date are always taken from the XML;
the server refuses (`400 mismatched_id`) a nodeset whose `<Model ModelUri>` does not match the group
the request addresses. If `name` is omitted it defaults to the namespace URI.

A successful upload returns `201 Created`, a `Location` header pointing at the new version's
`$details` URL, and the version entity as body. Uploads are **always private** (visible only to the
uploader and administrators) until approved through the normal Cloud Library approval workflow; this
is unchanged from `/infomodel/upload`.

**Overwrite.** If a nodeset with the same identifier already exists the upload is rejected with
`409 conflict` unless `xRegistry-overwrite: true` (header) or `?overwrite=true` (query) is set. The
same ownership rules as `/infomodel/upload?overwrite=true` apply (you cannot overwrite a published or
administrator-owned nodeset).

All writes are recorded in the DPP audit log (`Create`/`Modify`/`Delete`, with
`Attempted`/`Success`/`Failed`/`PartialFailure` outcomes), exactly like the `/infomodel` endpoints.

### 3.3 Errors

Errors are RFC 9457 problem details (`application/problem+json`):

```json
{
  "type": "https://xregistry.io/spec/core/errors/not_found",
  "status": 404,
  "title": "Not found",
  "instance": "/xregistry/nodesetgroups/abc",
  "detail": "Group 'abc' not found."
}
```

Error types used: `not_found`, `mismatched_id`, `invalid_data`, `bad_flag`, `action_not_supported`,
`conflict`, `forbidden`, `server_error`.

---

## 4. Authentication

The API uses the same `ApiPolicy` as `/infomodel`: HTTP Basic authentication, the signed-in browser
session, or (if `APIKeyAuth` is configured) an `X-API-Key` header. Reads return published nodesets
plus the caller's own unpublished uploads; administrators see every nodeset. Writes require an
authenticated user with write permission on the API key, if API keys are used.

---

## 5. Usage examples

```bash
BASE=https://uacloudlibrary.opcfoundation.org/xregistry
AUTH="-u user:password"
GID='http~3A~2F~2Fopcfoundation.org~2FUA~2FDI~2F'   # http://opcfoundation.org/UA/DI/

# Discover the registry
curl $AUTH "$BASE"
curl $AUTH "$BASE/model"
curl $AUTH "$BASE/capabilities"

# List all namespaces (groups)
curl $AUTH "$BASE/nodesetgroups"

# Everything about one namespace, including all versions
curl $AUTH "$BASE/nodesetgroups/$GID?inline"

# Metadata of the latest DI nodeset
curl $AUTH "$BASE/nodesetgroups/$GID/nodesets/nodeset\$details"

# Download the latest DI NodeSet2 XML
curl $AUTH -o Opc.Ua.Di.NodeSet2.xml "$BASE/nodesetgroups/$GID/nodesets/nodeset"

# Download a specific revision (nodeset identifier 2844662655)
curl $AUTH -o Opc.Ua.Di.1.03.NodeSet2.xml "$BASE/nodesetgroups/$GID/nodesets/nodeset/versions/2844662655"

# Upload a new revision with metadata in headers
curl $AUTH -X PUT "$BASE/nodesetgroups/$GID/nodesets/nodeset" \
     -H "Content-Type: application/xml" \
     -H "xRegistry-name: OPC UA for Devices" \
     -H "xRegistry-license: MIT" \
     -H "xRegistry-copyright: (c) OPC Foundation" \
     -H "xRegistry-keywords: devices,di" \
     --data-binary @Opc.Ua.Di.NodeSet2.xml

# Upload with a JSON entity body
curl $AUTH -X PUT "$BASE/nodesetgroups/$GID/nodesets/nodeset\$details" \
     -H "Content-Type: application/json" \
     -d '{ "name": "OPC UA for Devices", "license": "MIT", "copyright": "(c) OPC Foundation",
           "keywords": ["devices","di"], "nodeset": "<UANodeSet ...>...</UANodeSet>" }'

# Replace an existing unpublished upload
curl $AUTH -X PUT -H "xRegistry-overwrite: true" -H "Content-Type: application/xml" \
     --data-binary @Opc.Ua.Di.NodeSet2.xml "$BASE/nodesetgroups/$GID/nodesets/nodeset"

# Delete one revision you own
curl $AUTH -X DELETE "$BASE/nodesetgroups/$GID/nodesets/nodeset/versions/2844662655"
```

Full-registry export for mirroring: `GET $BASE/export`.

---

## 6. Implementation overview

| File | Role |
|---|---|
| `UACloudLibraryServer/Controllers/XRegistryController.cs` | All routes, `$details`/document handling, `xRegistry-*` headers, upload validation, authorization, auditing |
| `UACloudLibraryServer/XRegistry/XRegistryIds.cs` | Constants, spec version, group-id encoding/decoding, id validation, RFC 3339 timestamps |
| `UACloudLibraryServer/XRegistry/XRegistryInline.cs` | Parsing and scoping of the `?inline` flag |
| `UACloudLibraryServer/XRegistry/XRegistryError.cs` | RFC 9457 problem-details results |
| `UACloudLibraryServer/XRegistry/XRegistryEntityBuilder.cs` | `XRegistryGroup` (grouping of nodesets by namespace) and the JSON builders for root, model, capabilities, group, resource, meta and version entities |
| `UACloudLibraryServer/XRegistry/IXRegistryNodesetStore.cs` / `XRegistryNodesetStore.cs` | Data-access abstraction over `CloudLibDataProvider` and `DbFileStorage`; the interface allows the controller to be unit tested without a database |
| `UACloudLibraryServer/CloudLibDataProvider.cs` | `GetAllNodesetMetadataAsync` – bulk metadata query the registry is built from |
| `Tests/UACloudLibraryServer.UnitTests/XRegistry*Tests.cs`, `FakeXRegistryNodesetStore.cs` | Unit tests (ids, inline flag, entity shapes, controller behaviour) |

The registry is computed on each request from the nodeset metadata table; no additional tables or
migrations were introduced.

---

## 7. Deviations from the xRegistry specification

The following points deviate from, or only partially implement, xRegistry core 1.0-rc2. They are
deliberate trade-offs to fit the existing Cloud Library data model and deployment.

1. **Entry point is `/xregistry`, not the server root.**
   The specification places the registry entity at the root of the server (`GET /`). The Cloud
   Library root is the web UI and the other APIs (`/infomodel`, `/graphql`, DPP) live beside it, so
   the registry is mounted under `/xregistry`. Clients must use `https://<host>/xregistry` as the
   registry base URL; all `self`/`*url` attributes are absolute and already include the prefix.

2. **Fixed, single resource per group.**
   xRegistry allows any number of resources in a group. Here every group has exactly one resource
   with the constant id `nodeset`. Requests for other resource ids return `404`, and groups cannot be
   created or renamed directly (`PUT /nodesetgroups/{gid}` → `405`); they appear and disappear with
   their nodesets.

3. **Version ids are server-assigned and immutable** (`setversionid: false`, `versionmodes: ["manual"]`).
   The version id is the Cloud Library nodeset identifier, a hash computed from the NodeSet2 content.
   Clients cannot choose a version id, and `PUT …/versions/{vid}` is therefore not offered; new
   versions are created with `PUT …/nodeset` or `POST …/versions`.

4. **Default version is implicit and sticky to the latest publication date.**
   `defaultversionsticky` is reported as `true`, but `?setdefaultversionid` and
   `PATCH/PUT …/meta` are not supported; the default is always the version with the newest
   `publicationdate`, not the most recently uploaded one.

5. **Epochs and timestamps are derived, not stored** (see §1.2).
   Consequently the `?epoch=` optimistic-concurrency flag is not enforced on writes, and `epoch`
   values change when sibling versions are added or removed rather than when the entity itself is
   modified.

6. **Metadata updates require re-upload.**
   `PATCH` on any entity and `PUT` of metadata-only changes are not supported. Changing title,
   license, keywords etc. means re-uploading the nodeset with `overwrite` set (subject to the
   ownership rules). Uploaded nodesets are private until approved; the approval workflow is outside
   the xRegistry API.

7. **No `?filter`, `?sort`, pagination or `?doc` flag.**
   `pagination: false` is advertised in the capabilities; `GET /nodesetgroups` and
   `…/versions` always return the complete map. Use the `/infomodel/find` or GraphQL endpoints for
   keyword search. The `?doc` (document view), `?collections` and `?binary` flags are not
   implemented; `inline`, `specversion` and `$details` are.

8. **`inline=*` excludes documents, model and capabilities.**
   The spec treats `inline` without a value as "everything". To keep responses bounded (a full
   nodeset can be many megabytes) `inline`/`inline=*` inlines all collections but not the
   `nodeset` documents, `model` or `capabilities`; these must be named explicitly. `GET /export`
   includes model and capabilities but still not the documents.

9. **Upload side effects beyond the spec.**
   Document `GET`s increment the Cloud Library download counter; writes are recorded in the DPP audit
   log; `overwrite` is a Cloud Library-specific header/query flag with no equivalent in xRegistry.
   The `name` attribute defaults to the namespace URI when omitted.

10. **Access control is per nodeset, not per entity tree.**
    Visibility of unpublished nodesets depends on the caller, so two users can see a different set of
    groups and versions, and `epoch`/`*count` values differ accordingly. `DELETE` of a group/resource
    requires the administrator role even if the caller owns every version.

11. **Schemas, `shortself`, `capabilities` writes.**
    `schemas` is empty (no schema validation of documents), `shortself` is `false`, and the
    capabilities and model are read-only (`PUT /capabilities`, `PUT /model` are not offered).

---

## 8. Road map to full xRegistry compliance

This chapter lists what would have to change in the Cloud Library to remove the deviations of §7
and pass the xRegistry core conformance tests. Items are grouped by the layer they affect; the
numbers in brackets refer to the deviation they resolve. Items marked *(breaking)* change the
visible behaviour of the Cloud Library beyond the xRegistry API.

### 8.1 Hosting and entry point

* **Serve the registry at the server root** [1]. Either host the xRegistry API on its own
  hostname/port (e.g. `xregistry.uacloudlibrary.opcfoundation.org`, a second Kestrel endpoint or a
  reverse-proxy path rewrite that strips `/xregistry`), or let `GET /` content-negotiate: return the
  registry entity for `Accept: application/json` and the Blazor UI otherwise. The simplest
  non-breaking option is a dedicated hostname mapped to the `/xregistry` prefix by the proxy; the
  `XRegistryEntityBuilder` base URL then has to be configurable (`XRegistry:BaseUrl`) instead of
  derived from the request. Also add `/model`, `/capabilities`, `/export` and `/nodesetgroups` at the
  root for that host.
* **Add `OPTIONS` / CORS support** so browser-based xRegistry tools can call the API, and expose
  `Access-Control-Expose-Headers` for the `xRegistry-*` headers.

### 8.2 Data model and database

* **Persist xRegistry entity state** [5, 6]. Add tables (or columns on `NamespaceMetaData`) for:
  * per-entity `epoch` counters for registry, group, resource, meta and version;
  * `createdat` / `modifiedat` per entity, updated on every write;
  * group-level `name`, `description`, `documentation`, `labels` (today taken from the default
    version);
  * resource `meta`: `defaultversionid`, `defaultversionsticky`, `readonly`, `compatibility`,
    `compatibilityauthority`, `deprecated`;
  * explicit `ancestor` per version, so the chain survives re-uploads and manual default changes.
  This is an EF Core migration plus a one-off back-fill from the derived values in §1.2.
* **Store arbitrary extension attributes** [6]. xRegistry permits client-defined extension
  attributes on every entity (`"*": { "type": "any" }`). Add a JSONB column per entity to hold
  attributes that are not mapped to a dedicated column, and declare the extension policy in the
  model.
* **Make the version id independent of the content hash** [3]. The Cloud Library identifier is a
  hash of `ModelUri` + `PublicationDate`; xRegistry expects client-chosen or server-generated ids
  that are stable even when the content changes. Keep the hash as an internal key (or as the
  `nodesetidentifier` extension attribute), add a separate `versionid` column, and allow
  `PUT …/versions/{vid}` with a client-chosen id (`setversionid: true`). *(breaking for clients
  that equate version id with `/infomodel` identifier.)*
* **Allow several resources per group** [2]. If the OPC Foundation wants to publish other artefacts
  per namespace (documentation PDFs, JSON schemas, companion-spec test files, OPC UA FX descriptors),
  the fixed `nodeset` resource id has to become a real `nodesets` collection keyed by a resource id,
  with `NodeSetModel` linked to a resource row rather than directly to the group.

### 8.3 API surface

* **Entity writes** [4, 6]:
  * `PATCH` on registry, group, resource, meta and version with JSON merge semantics;
  * `PUT`/`PATCH …/meta` to change `defaultversionid`, `defaultversionsticky`, `readonly`,
    `compatibility`, `deprecated`;
  * `?setdefaultversionid=<vid>` on version `PUT`/`POST`;
  * `PUT`/`POST` on `/nodesetgroups` and `/nodesetgroups/{gid}` to create or update groups
    (metadata-only, before the first nodeset exists) and `PATCH` of group attributes;
  * bulk `POST /nodesetgroups` and `POST …/nodesets` with a map of entities, and `POST /` to import a
    whole registry tree (the counterpart of `GET /export`);
  * the `?nested` flag to accept inlined collections in a write body.
* **Optimistic concurrency** [5]: honour `?epoch=` (and `?noepoch`) on every write and return
  `412 mismatched_epoch` when it differs; include `epoch` in all write responses.
* **Query flags** [7]:
  * `?filter=<path>=<value>` with the spec's `=`, `!=`, `<>`, `<`, `<=`, `>`, `>=`, wildcard and
    multi-filter (AND/OR) semantics on any attribute, including nested paths such as
    `nodesets.versions.publicationdate`;
  * `?sort=<attribute>[=asc|desc]`;
  * `?doc` (document view: relative URLs and `$details` suffixes stripped);
  * `?collections` (return only the collection URLs/counts) and `?binary` (base-64 document inlining);
  * `?specversion=<v>` negotiation with `400 unsupported_specversion` for unknown versions.
* **Pagination** [7]: `Link: <…>; rel="next"` headers on `/nodesetgroups`, `…/nodesets` and
  `…/versions`, driven by a server-chosen page size, plus `Link: rel="first"/"prev"/"last"` and the
  total count. Advertise `pagination: true`.
* **Full `inline` semantics** [8]: `inline` / `inline=*` must include documents, `model` and
  `capabilities` as the spec requires. Either implement it exactly and rely on pagination to bound
  responses, or keep the current behaviour and advertise it as a documented
  `inline` limitation in the capabilities (`"flags": [...]` with a custom
  `inlinelimit` extension); only the former is fully compliant.
* **Version `GET`s without side effects** [9]: stop counting downloads on xRegistry document
  reads, or count them only when `?xregistry_count=true` (a Cloud Library extension) is passed.
* **Remove the `overwrite` extension** [9]: with persisted epochs and client-chosen version ids,
  `PUT …/versions/{vid}` + `?epoch=` replaces it. Keep `xRegistry-overwrite` for one deprecation
  cycle.
* **Capabilities and model management** [11]: `PUT /capabilities` (at least to toggle
  `pagination`, `shortself` and the `apis`/`flags` lists within what the server supports) and
  `PUT /model` to register additional groups/resources and extension attributes, both admin-only.
  Serve `/model` with `$schema`, `labels`, `attributes` for the registry itself, and emit
  `modelsource` as uploaded.
* **`shortself`** [11]: optional, but if advertised, implement a stable short-URL table
  (`/s/<id>`) and return the short URLs in `self` when `shortself: true`.
* **Schemas** [11]: declare the NodeSet2 XSD under `schemas`
  (`"xsd/http://opcfoundation.org/UA/2011/03/UANodeSet.xsd"`), validate uploaded documents against
  it and return `400 invalid_data` on failure.
* **Error catalogue**: return every applicable spec error id (`mismatched_epoch`,
  `unsupported_specversion`, `data_retrieval_error`, `header_decoding_error`, `missing_versions`,
  `multiple_roots`, `readonly`, `too_many_versions`, `unknown_attribute`, `unknown_id`, …) and the
  spec's `title` wording, instead of the current subset.

### 8.4 Authorization and workflow

* **Model the approval workflow in xRegistry terms** [6, 10]. The private-until-approved state has
  no spec equivalent. Options: expose unpublished versions only to their owner **and** mark them
  with a `published: false` extension attribute (current behaviour, documented); or make approval
  a `PATCH …/meta` of a `published` meta attribute by an administrator so the whole lifecycle is
  reachable through the API; or keep unpublished nodesets out of the registry entirely and leave
  submission to `/infomodel/upload`. The second option is the one that keeps the API self-contained.
* **Uniform access rules**: let owners delete a group/resource when they own every version, and
  return `403 forbidden` consistently (the spec reserves `401`/`403` semantics to the deployment,
  but conformance tests expect them to be uniform across entity levels).
* **Anonymous reads** [4]: the spec expects unauthenticated `GET`s of public data to work. The
  `ApiPolicy` currently requires an authenticated identity; add an `AllowAnonymous` read path (as the
  DPP public read endpoints already do) that is restricted to published nodesets.

### 8.5 Verification

* Run the official conformance test suite (the `xreg` / `xrserver` test harness from
  `github.com/xregistry/server`) against a Cloud Library instance in CI, alongside the existing
  `CloudLibClientTests`.
* Add integration tests in `Tests/CloudLibClientTests` for the end-to-end upload → approval →
  default version → delete flow once the write surface above exists; the current unit tests cover
  routing and entity shape only.
* Publish the registry's `GET /model` as part of the release so downstream xRegistry clients can
  code-generate against it, and add the xRegistry routes to the Swagger document group
  (`SwaggerDoc("xregistry", …)`) so the OpenAPI description matches the spec's resource naming.

### 8.6 Suggested order

1. Persist epochs/timestamps/meta (8.2) and enforce `?epoch` (8.3) – this unblocks every write
   feature and is invisible to current clients.
2. `PATCH`/`meta` writes and `setdefaultversionid`.
3. `filter`, `sort`, pagination and full `inline`.
4. Client-chosen version ids and multiple resources per group *(breaking; align with a major
   release)*.
5. Root-level hosting and anonymous reads.
6. Conformance suite in CI.
