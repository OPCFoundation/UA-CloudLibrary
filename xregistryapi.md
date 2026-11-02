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
