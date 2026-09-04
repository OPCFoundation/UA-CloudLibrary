# Release Notes

## 2026-08-23 — `IsPublished` flag & catalog read-path performance work (branch `CM-2606-IsPublished`)

### Data model
- Added `NamespaceMetaDataModel.IsPublished` (`bool`, defaults to `false`).
- Startup migration (`Migrations/Scripts/AddIsPublishedToNamespaceMeta.sql`) is embedded in the assembly and run on startup only when the required schema objects are missing:
  - Adds the `IsPublished` column (`boolean NOT NULL DEFAULT false`).
  - Back-fills `IsPublished = true` for all existing rows whose `UserId = 'admin'`.
  - Drops the naive `IX_NamespaceMeta_IsPublished` index (poor selectivity on a highly skewed boolean).
  - Creates partial index `IX_NamespaceMeta_Published_Nodeset` on `("NodesetId") WHERE "IsPublished" = true` — targets the anonymous public catalog hot path (enables index-only scans).
  - Creates partial index `IX_NamespaceMeta_UserId` on `("UserId") WHERE "UserId" IS NOT NULL` — targets per-user "my uploads" lookups.
- Startup gate now checks for the presence of `IX_NamespaceMeta_Published_Nodeset` in `pg_indexes` so previously-migrated databases automatically pick up the new indexes.

### Access control refactor (`CloudLibDataProvider.cs`)
- Replaced every record-level `UserId == "admin"` visibility check with `IsPublished`.
- Removed all remaining `userId == "admin"` / `userId != "admin"` role short-circuits — access is now purely record-driven.
- `GetMetadataUserFilter` / `GetNodesetUserFilter` split into two shapes:
  - **Anonymous fast path** (`userId` empty): predicate collapses to `IsPublished` (or `Metadata.IsPublished`), matching the partial index one-to-one.
  - **Authenticated path**: `IsPublished || UserId == :userId || UserId IS NULL`.
- Added `AsNoTracking()` to the read-only query entry points in `GetNodeSets` and `GetNodeModels` to eliminate change-tracker overhead on catalog reads.

### Optional materialized view (feature-flagged: `EnableMatView`)
- New scripts `CreateNamespaceMetaPublicView.sql` / `DropNamespaceMetaPublicView.sql` (embedded resources).
- Materialized view `NamespaceMetaPublic` mirrors `NamespaceMeta WHERE IsPublished = true`, with:
  - `UNIQUE INDEX (NodesetId)` — required for `REFRESH MATERIALIZED VIEW CONCURRENTLY`.
  - Secondary indexes on `Title` and `CreationTime DESC`.
- Startup (`EnsurePublicMaterializedViewAsync`):
  - If `EnableMatView` is set: creates the view (idempotent) and runs an initial `REFRESH`.
  - If unset: drops the view so toggling the flag off is honored.
- New `PublicMaterializedViewRefreshTask` background service:
  - Runs `REFRESH MATERIALIZED VIEW CONCURRENTLY` every `MatViewRefreshSeconds` (default `60`).
  - No-op when `EnableMatView` is unset — zero cost.

### Configuration (via `.env` / environment variables)
| Key | Default | Effect |
|---|---|---|
| `EnableMatView` | (unset) | If set to any non-empty value, provisions and periodically refreshes `NamespaceMetaPublic`. |
| `MatViewRefreshSeconds` | `60` | Refresh interval for the background refresh service. |

### Expected performance impact

Rough guidance based on the query shape (`NamespaceMeta` scan/join filtered by visibility predicate) — actual numbers depend on hardware, cache state, and query complexity.

| Scale | Anonymous catalog listing / search | Authenticated "my uploads" | Public detail lookup by `NodesetId` |
|---|---|---|---|
| **Small (<1K rows)** | No measurable change. Planner seq-scans regardless. | No measurable change. | No change (unique index already handles this). |
| **Medium (~5K rows)** | **~2–5× faster** on unfiltered browse/search: partial index + `AsNoTracking()` remove per-row string compares and change-tracker cost. Latency drops from tens of ms into low single-digit ms range. | **~2–3× faster** due to partial `IX_NamespaceMeta_UserId` and the simpler predicate. | Effectively unchanged, but memory pressure per request is lower. |
| **Large (>1M rows)** | **10× or more** in the common case: anonymous predicate collapses to a single index-only scan on `IX_NamespaceMeta_Published_Nodeset` instead of scanning `NamespaceMeta` and filtering. With `EnableMatView` on, another **2–5×** on top: browse/search hits a compact matview with its own `Title` / `CreationTime` indexes and skips the `NamespaceMeta ↔ NodeSet` join. | **~5–10× faster** for filtered "my uploads" — partial `IX_NamespaceMeta_UserId` is small and cache-hot. | Unchanged (already O(1) via PK/unique index). |

Write path costs:
- Uploads/publishes: **negligible** overhead from the two partial indexes (they only include the subset each write touches). With `EnableMatView` on, add the periodic refresh cost (bounded, non-blocking readers thanks to `CONCURRENTLY`); on write-heavy bursts the view may lag by up to `MatViewRefreshSeconds`.

### Files touched
- `Models/NamespaceMetaDataModel.cs` — new `IsPublished` property.
- `Migrations/Scripts/AddIsPublishedToNamespaceMeta.sql` — new (embedded).
- `Migrations/Scripts/CreateNamespaceMetaPublicView.sql` — new (embedded).
- `Migrations/Scripts/DropNamespaceMetaPublicView.sql` — new (embedded).
- `UA-CloudLibrary.csproj` — added `<EmbeddedResource Include="Migrations\Scripts\*.sql" />`.
- `Startup.cs` — added `EnsureIsPublishedColumnAsync`, `EnsurePublicMaterializedViewAsync`, `LoadEmbeddedSql`, and the `PublicMaterializedViewRefreshTask` hosted service; wired into `CloudLibStartupTask`.
- `CloudLibDataProvider.cs` — filter refactor, admin-string removal, `AsNoTracking()` on read paths.

## 2026-08-22

### Explorer page (`UACloudLibraryServer/Components/Pages/Explorer.razor`)

**Search results UI overhaul**
- Replaced card-tile results with a modern responsive table (Nodeset, License+Version, Published, Downloads, Actions).
- Row actions: **View**, **Download**, **More** (icon buttons).
- Removed the *Owner* and *Description* columns; License and Version merged into a single pill/badge.
- Fixed Bootstrap 4 spacing (`mr-*` / `ml-*` instead of BS5 utilities), truncation, and horizontal scroll.

**Details / information panel**
- Added a details card below the results that appears when a row is selected.
- Selected details include Description, Copyright, Keywords, Locales, and a Links section (Documentation, License, Release notes, Test specification, Purchasing information).
- Details card auto-closes when a search returns zero results.

**"Reference:" quick link**
- When the user searches with a `type:<term>` filter and the selected nodeset's Documentation URL starts with `https://reference.opcfoundation.org/`, an additional **Reference: `<term>`** link button is shown pointing to `https://reference.opcfoundation.org/search?q=<term>&tab=node`.

**Search & pagination state**
- Search keywords, current page, and page size are persisted in the URL (`q`, `page`, `size`) via `RestoreStateFromUrl()` / `UpdateUrl()` so the browser Back button restores the previous search.
- Search help modal (`?` button) documents `name:`, `publisher:`, `type:`, `license:`, `exact:`, `date:`, `sort:` filters.
- **Items per page** moved to the right of the search input group, with a compact 2-digit-wide numeric field.

**Forwarding search to the browser page**
- `ViewFileFromURL` extracts any `type:<term>` keyword and forwards it as `search=type:<term>` on the `/browser?...` URL.

### Browser page (MVC → Blazor plumbing)

**`Controllers/BrowserController.cs`, `Models/BrowserModel.cs`, `Views/Browser/Index.cshtml`**
- Added a new `search` URL/query parameter that flows through the controller → model → view → `TreePage` component.

### Tree page (`UACloudLibraryServer/Components/Pages/TreePage.razor`)

**New `[Parameter] Search`**
- Reads the URL search parameter and, after the initial expand, automatically runs `SearchTree()` with that value.

**Search bar & Nodeset Browser layout**
- Added a **Nodeset scope** dropdown above the search input, populated with `_client.LoadedNamespaces` (default: **All**).
- Added a `?` help button before the input, with a modal describing the syntax (styled to match the Explorer help).
- Added a **Find all** checkbox and **Find next** button under the search input.

**Search syntax**
- `id:<node id>` — exact case-insensitive match against the identifier suffix of the NodeId (part after the last `=`, e.g. `2258` from `nsu=...;i=2258`).
- `type:<term>` — case-insensitive substring on the node's `BrowseName`.
- `name:<term>` — case-insensitive substring on the node's `DisplayName`.
- No prefix — case-insensitive, accent-insensitive substring on **BrowseName OR DisplayName**.

**Search traversal**
- Breadth-first search over the tree with the `Types` subtree prioritized (deque prepending) so type-child variables aren't starved by huge `Objects` subtrees.
- Cap raised (`SearchMaxNodes = 50000`).
- **Nodeset scope** restricts matches (and bolding) to nodes whose `nsu=` URI starts with the selected scope.
- Previous selection and match highlights are cleared when a new search starts.

**Find all / Find next**
- **Find all** off (default): search stops at the first match — fast.
- **Find all** on: collects every match; **Find next** cycles through them (status shows "Match N of M").
- Without **Find all**: **Find next** re-runs the search excluding previously-shown match Ids, so it always advances to the next unseen match.

**Match highlighting**
- All matching nodes are visually highlighted (blue text on a light-yellow background). The currently selected node still uses red text.
- Bold rendering for nodeset-owned nodes (`nsu=...`) now also honors the selected Nodeset scope — only in-scope nodes are bolded.

**Selected Node Information card**
- Added a new card in the right column (above the *Selected Node Value* card) showing Display Name, **Browse Name**, Node Id, Value, and child-node count for the currently selected node.

**Robustness fixes**
- `OnNodeExpand` is null- and exception-safe (never assigns `null` to `Children`, catches server errors).
- Removed `@bind-ExpandedNodes` in favor of a plain `ExpandedNodes` parameter to avoid recursive re-render loops.

### Models

**`Models/NodesetViewerNode.cs`**
- Added `BrowseName` property.
- Null-safe `CompareTo`, `Equals`, `GetHashCode` (using `Id ?? string.Empty`).

**`UAClientServer/UAClient.cs`**
- `GetChildren` now populates `BrowseName` from `description.BrowseName?.Name` in addition to the existing DisplayName.

### Notable behavior changes

- Explorer Back button returns to the previous search state.
- `/browser?...&search=<term>` deep-linking runs the tree search on load.
- `/browser?...&search=type:<term>` from Explorer opens the browser scoped to that type.
- Tree search finds nodes deep inside type hierarchies (e.g., `ElectrolyzerFlowRate` when searching `flow`).
- Tree search is fast by default (single-match), with an opt-in exhaustive mode.
