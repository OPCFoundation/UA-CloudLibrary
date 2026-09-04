# Release Notes

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
