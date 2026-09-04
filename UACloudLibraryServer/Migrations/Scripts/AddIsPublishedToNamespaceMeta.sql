-- Adds the IsPublished column to the NamespaceMeta table and provisions
-- indexes optimized for large tables (millions of rows).
-- IsPublished is set to true for rows where UserId is 'admin', false otherwise.

ALTER TABLE "NamespaceMeta"
    ADD COLUMN IF NOT EXISTS "IsPublished" boolean NOT NULL DEFAULT false;

UPDATE "NamespaceMeta"
   SET "IsPublished" = true
 WHERE "UserId" = 'admin';

-- The naive boolean index is not selective enough on a highly skewed column.
DROP INDEX IF EXISTS "IX_NamespaceMeta_IsPublished";

-- Hot path: anonymous / public catalog browsing. Small, cache-friendly,
-- allows index-only scans on NodesetId for published rows.
CREATE INDEX IF NOT EXISTS "IX_NamespaceMeta_Published_Nodeset"
    ON "NamespaceMeta" ("NodesetId")
    WHERE "IsPublished" = true;

-- Per-user filter: "my uploads". Excludes orphaned rows to keep the index lean.
CREATE INDEX IF NOT EXISTS "IX_NamespaceMeta_UserId"
    ON "NamespaceMeta" ("UserId")
    WHERE "UserId" IS NOT NULL;
