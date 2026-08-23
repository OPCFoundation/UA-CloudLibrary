-- Removes the NamespaceMetaPublic materialized view. Used when the
-- "EnableMatView" feature flag is turned off.

DROP MATERIALIZED VIEW IF EXISTS "NamespaceMetaPublic";
