-- Assigns the privileges the runtime app role needs to operate the
-- NodeSetEditor app AND to run EF Core's Database.MigrateAsync() at
-- startup: CONNECT on this database, USAGE/CREATE on schema public, DML on
-- all tables and sequences, ownership of all existing tables/sequences/views
-- (so migrations can ALTER/DROP them -- Postgres has no ACL-level "ALTER"
-- privilege, DDL on an existing object requires owning it), plus default
-- privileges so future objects created by dbadmin (e.g. via manual DDL)
-- inherit the same grants.
--
-- The role itself must already exist; it is created manually
-- (Postgres roles are cluster-wide, so creating it once per server is enough).
-- Run as dbadmin against the target database.
--
-- The app role name is taken from the psql variable ':app_user'. Pass it
-- explicitly with -v app_user=<role> on the psql command line; if omitted
-- it defaults to 'webapp' for backwards compatibility with callers that
-- haven't been updated to pass the parameter.
--
--   psql ... -v app_user=webapp -f grant_webapp.sql

\set ON_ERROR_STOP on

-- Default :app_user to 'webapp' when the caller did not supply -v app_user=<role>.
\if :{?app_user}
\else
\set app_user webapp
\endif

SELECT current_database() AS dbn \gset

GRANT CONNECT ON DATABASE :"dbn" TO :"app_user";
GRANT USAGE, CREATE ON SCHEMA public TO :"app_user";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES    IN SCHEMA public TO :"app_user";
GRANT USAGE, SELECT                  ON ALL SEQUENCES IN SCHEMA public TO :"app_user";

ALTER DEFAULT PRIVILEGES FOR ROLE dbadmin IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"app_user";
ALTER DEFAULT PRIVILEGES FOR ROLE dbadmin IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO :"app_user";

-- Let dbadmin act with the app role's privileges, which is required in
-- order to change ownership of objects to it below. dbadmin has admin
-- option on :app_user already, since dbadmin created the role.
GRANT :"app_user" TO CURRENT_USER;

-- Transfer ownership of every existing table/sequence/view in schema public
-- to the app role, so that EF Core migrations run by the app (CREATE TABLE,
-- ALTER TABLE ADD/DROP COLUMN, CREATE/DROP INDEX, DROP TABLE, etc.) succeed.
-- New objects created later by the app (e.g. future migrations) are already
-- owned by it automatically, since Postgres makes the creator the owner.
--
-- Generates one ALTER ... OWNER TO statement per relation and runs each via
-- psql's \gexec (a DO $$ ... $$ block won't work here: psql does not
-- interpolate ':app_user' inside dollar-quoted bodies).
--
-- Identity/serial sequences (deptype 'a' or 'i' in pg_depend) are linked to
-- their owning table column and Postgres refuses to reassign their owner
-- directly ("cannot change owner of sequence ... is linked to table ...");
-- they're excluded here and instead follow automatically when their parent
-- table's owner changes below.
SELECT format(
    'ALTER %s public.%I OWNER TO %I',
    CASE c.relkind
        WHEN 'S' THEN 'SEQUENCE'
        WHEN 'v' THEN 'VIEW'
        ELSE 'TABLE'
    END,
    c.relname,
    :'app_user'
)
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'public'
  AND c.relkind IN ('r', 'p', 'S', 'v')
  AND NOT (
        c.relkind = 'S'
        AND EXISTS (
            SELECT 1
            FROM pg_depend d
            WHERE d.classid = 'pg_class'::regclass
              AND d.objid = c.oid
              AND d.deptype IN ('a', 'i')
        )
  )
\gexec

-- Give dbadmin default privileges on objects the app creates going forward
-- (e.g. new tables added by future migrations), so admin tooling like
-- pgAdmin keeps full visibility without needing further manual grants.
ALTER DEFAULT PRIVILEGES FOR ROLE :"app_user" IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO dbadmin;
ALTER DEFAULT PRIVILEGES FOR ROLE :"app_user" IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO dbadmin;
