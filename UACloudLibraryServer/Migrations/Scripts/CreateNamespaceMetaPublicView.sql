-- Creates a materialized view over the published subset of NamespaceMeta.
-- Used by anonymous/public catalog reads when the "EnableMatView" feature flag
-- is enabled. Refreshable CONCURRENTLY thanks to the unique index on NodesetId.

CREATE MATERIALIZED VIEW IF NOT EXISTS "NamespaceMetaPublic" AS
SELECT *
  FROM "NamespaceMeta"
 WHERE "IsPublished" = true
WITH NO DATA;

-- Required for REFRESH MATERIALIZED VIEW CONCURRENTLY.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_NamespaceMetaPublic_NodesetId"
    ON "NamespaceMetaPublic" ("NodesetId");

-- Support common browse/search predicates.
CREATE INDEX IF NOT EXISTS "IX_NamespaceMetaPublic_Title"
    ON "NamespaceMetaPublic" ("Title");

CREATE INDEX IF NOT EXISTS "IX_NamespaceMetaPublic_CreationTime"
    ON "NamespaceMetaPublic" ("CreationTime" DESC);
