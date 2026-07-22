$pdump = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe" 
$hostname = "uacloudlib-prod.postgres.database.azure.com"
$database = "uacloudlib"
$adminUser = "opcua"
$port = 5432

if (-not $env:PGPASSWORD) {
    Write-Host 'No Password set. Call: $env:PGPASSWORD = <password>'
	exit 1
}

& $pdump -h $hostname -p $port -b -v --no-owner --no-privileges -U $adminUser -d $database -f ".\$database-backup.sql"