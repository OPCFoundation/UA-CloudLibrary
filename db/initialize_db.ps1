param(
    [string]$Server = 'localhost',
    [string]$Database = 'uacloudlib',
    [string]$AdminUser = 'dbadmin',
    [string]$AppUser = 'uacloudlibrary-app',
    # Drop and recreate the database before applying the EF Core schema. Use
    # this when DbContext column types changed (EF Core's EnsureCreated only
    # creates missing tables; it does not alter existing ones).
    [switch]$Reset
)

$ErrorActionPreference = 'Stop'

$port = 5432
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'

if (-not $env:PGPASSWORD) {
    Write-Host 'No password set. Call: $env:PGPASSWORD = <password>' -ForegroundColor Yellow
    exit 1
}

$repoRoot  = Split-Path -Parent $PSScriptRoot

# Prefer encrypted connections, but fall back if the local server has no SSL.
$env:PGSSLMODE = 'prefer'
$connStr = "Server=$Server;Database=$Database;Port=$port;User Id=$AdminUser;Password=$env:PGPASSWORD;Ssl Mode=Prefer;"

if ($Reset) {
    Write-Host "Dropping database '$Database' on $Server (Reset=true) ..." -ForegroundColor Yellow
    $dropDbSql = @"
SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$Database' AND pid <> pg_backend_pid();
DROP DATABASE IF EXISTS "$Database";
"@
    $dropDbSql | & $psql -h $Server -p $port -U $AdminUser -d postgres -v ON_ERROR_STOP=1
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "Ensuring database '$Database' exists on $Server ..." -ForegroundColor Cyan
$ensureDbSql = @"
SELECT 'CREATE DATABASE "$Database"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$Database')\gexec
"@
$ensureDbSql | & $psql -h $Server -p $port -U $AdminUser -d postgres -v ON_ERROR_STOP=1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
