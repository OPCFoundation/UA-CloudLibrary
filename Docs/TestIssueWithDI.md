# Test issue: `CloudLibSearch.Search(["di"])` returns 40 instead of 42

**Test:** `Tests/CloudLibClientTests/CloudLibIntegrationTest.cs`, `CloudLibSearch.Search` (line 71)
**Branch:** `CM-2610-i315`

## Symptom

Four `Search` theory cases fail, each short by exactly two nodesets:

| Keywords | Expected | Actual |
|---|---|---|
| `null` | 64 | 62 |
| `["di"]` | 42 | 40 |
| `["robotics", "di"]` | 42 | 40 |
| `["robotics", "di", "pump", "plastic"]` | 43 | 41 |

Because the `null` (no keyword) case also fails, this is not a keyword-matching
problem: two nodesets are missing from every result set.

## What was ruled out

- `CloudLibDataProvider.SearchNodesets` / `ApplyKeywordFilters` are identical to `main`.
- `GetNodesetUserFilter` behaves the same for the test user (`admin`); all 63
  `NodeSets` rows are owned by `admin` and therefore visible.

## Root cause

### 1. Two nodesets are only half-ingested in the test database

| NodesetId | Title | `NamespaceMeta` | `DbFiles` blob | `NodeSets` row |
|---|---|---|---|---|
| 1277820317 | IO-Link (`http://opcfoundation.org/UA/IOLink/`) | yes | yes | **missing** |
| 3080400592 | IO-Link-IODD (`http://opcfoundation.org/UA/IOLink/IODD/`) | yes | yes | **missing** |

Totals: 65 `NamespaceMeta` rows, 65 `DbFiles` rows, 63 `NodeSets` rows
(62 distinct `ModelUri`s, since `testnodeset001` exists in two versions).

`SearchNodesets` queries from `_dbContext.NodeSetsWithUnapproved`, so a nodeset
without a `NodeSets` row is invisible to every search. Both IO-Link nodesets match
`di` (via "Device"/"DI" in their text), which explains 42 → 40 and 64 → 62.

### 2. Why indexing failed for IO-Link

Re-uploading the two files with `overwrite = true` reproduces the original error:

```
IOLink.IODD: Nodeset was stored but could not be indexed:
  Encountered end element 'http://opcfoundation.org/UA/2011/03/UANodeSet.xsd:Boolean'
  when expecting element 'http://opcfoundation.org/UA/2008/02/Types.xsd:Value'.

IOLink:      Nodeset was stored but could not be indexed:
  Encountered end element 'http://opcfoundation.org/UA/2011/03/UANodeSet.xsd:ListOfByte'
  when expecting element 'http://opcfoundation.org/UA/2008/02/Types.xsd:Value'.
```

The exception is thrown by `UANodeSet.Import(...)` inside
`NodeModelFactoryOpc.ImportNodeSet()` (`UACloudLibraryServer/NodeSetIndex/NodesetModelFactoryOpc.cs`).

The IO-Link test fixtures encode variable values **without the Types namespace**:

```xml
<Value>
    <Boolean>false</Boolean>
</Value>
```

The unqualified child inherits the `UANodeSet.xsd` default namespace. Compliant
nodesets (e.g. DI) qualify the value explicitly:

```xml
<Value>
    <String xmlns="http://opcfoundation.org/UA/2008/02/Types.xsd">1.03.0</String>
</Value>
```

This branch upgraded the OPC UA stack from `1.5.378.156` to `2.0.1`
(commit `68487cf5 Migration to OPC UA 2.0.1 nugets`). The 2.0.1 `XmlDecoder`
rejects the unqualified encoding strictly; the 1.5.x stack tolerated it, which is
why the expected counts (42 / 64) were correct before the upgrade.

### 3. Why the problem persists silently

`CloudLibDataProvider.UploadNamespaceAndNodesetWithResultAsync` writes the blob
(`_storage.UploadFileAsync`) and the metadata (`AddMetaDataAsync`) **before** it
indexes (`IndexNodeSetModelAsync`, ~lines 396–447). When indexing throws, the
blob and metadata stay in place and the upload returns
`FailedAfterStorageWrite(...)`.

On every subsequent test run the upload reaches the check at ~lines 328–336
("blob exists" and `RetrieveAllMetadataAsync` finds metadata) and returns
`Nodeset already exists. Use overwrite flag ...`. `UploadAndIndex.UploadNodeSets`
deliberately swallows that response (`TestConfig.IgnoreUploadConflict`), so:

- the two nodesets are never re-indexed, and
- no test fails at the upload stage; only the downstream `Search` counts fail.

## Possible fixes (none applied yet)

1. **Server-side tolerance** – before `_nodeset.Import(...)` (or in
   `CloudLibDataProvider.ReadUANodeSet`), move unqualified `<Value>` children into
   the `http://opcfoundation.org/UA/2008/02/Types.xsd` namespace. Makes the library
   robust to this common real-world encoding.
2. **Upload consistency** – when `IndexNodeSetModelAsync` fails, roll back the
   metadata/blob (or flag the nodeset for re-index) so a retry is not blocked by
   "Nodeset already exists".
3. **Test-only** – fix the namespaces in the two IO-Link fixture files under
   `Tests/CloudLibClientTests/TestNamespaces/`, and delete the orphaned rows
   (`NamespaceMeta` / `DbFiles` for `1277820317` and `3080400592`) from the test
   database so they are uploaded and indexed again.

## Useful diagnostics

```sql
-- metadata rows without an indexed NodeSets row
select m."NodesetId", m."Title"
from "NamespaceMeta" m
where not exists (select 1 from "NodeSets" n where n."Identifier" = m."NodesetId");
```

```powershell
# run against the docker-compose test database
Get-Content query.sql | docker exec -i db psql -U uacloudlib -d uacloudlib
```
