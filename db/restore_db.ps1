param(
    [string]$Server     = 'opcf-cloud-iop-db.postgres.database.azure.com',
    [string]$Database   = 'uacloudlib',
    [string]$BackupFile = '.\uacloudlib-backup.sql',
    [string]$AdminUser  = 'dbadmin',
    [string]$AppUser    = 'uacloudlibrary-app'
)

$ErrorActionPreference = 'Stop'

# Creates the target database if it doesn't exist, then restores a pg_dump
# SQL file (produced by .\backup_db.ps1) into it.
#
# Defaults assume the production Azure server with a fresh staging DB; pass
# -Server / -Database to point elsewhere. Admin user is fixed (dbadmin).

$psql      = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$adminUser = 'dbadmin'
$port      = 5432

if (-not $env:PGPASSWORD) {
    Write-Host 'No password set. Call: $env:PGPASSWORD = <password>' -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $BackupFile)) {
    Write-Host "Backup file not found: $BackupFile" -ForegroundColor Red
    Write-Host "Run .\backup_db.ps1 first to produce it." -ForegroundColor Yellow
    exit 1
}

# Prefer encrypted connections; falls back if the target has no SSL.
$env:PGSSLMODE = 'prefer'

Write-Host "Ensuring database '$Database' exists on $Server ..." -ForegroundColor Cyan
$ensureDbSql = @"
SELECT 'CREATE DATABASE "$Database"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$Database')\gexec
"@
$ensureDbSql | & $psql -h $Server -p $port -U $adminUser -d postgres -v ON_ERROR_STOP=1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Restoring '$BackupFile' into $Server / $Database ..." -ForegroundColor Cyan
& $psql -h $Server -p $port -U $adminUser -d $Database -v ON_ERROR_STOP=1 -f $BackupFile
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Applying grants for role '$AppUser' ..." -ForegroundColor Cyan
$grantSqlPath = Join-Path $PSScriptRoot 'grant_webapp.sql'
& $psql -h $Server -p $port -U $adminUser -d $Database -v "app_user=$AppUser" -f $grantSqlPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. $Server / $Database restored from $BackupFile." -ForegroundColor Green

# Print the runtime app's connection string. SSL mode follows the rule:
#   localhost (loopback) -> Prefer (local dev Postgres often has no SSL)
#   anything else        -> Require (Azure / staging / prod must use TLS)
$sslMode = if ($Server -in @('localhost', '127.0.0.1', '::1')) { 'Prefer' } else { 'Require' }
$appConnStr = "Server=$Server;Database=$Database;Port=$port;User Id=$AppUser;Password=<APP_PASSWORD>;Ssl Mode=$sslMode;"
Write-Host ""
Write-Host "App connection string (replace <APP_PASSWORD> with the role's password):" -ForegroundColor Cyan
Write-Host "  $appConnStr"
