# Applies EF Core migrations for every Postgres-backed service.
# Production apps keep ApplyMigrationsOnStartup=false; this script is the job the runtime comment refers to.
#
# Optional env (defaults match local compose on localhost:5433):
#   POSTGRES_HOST, POSTGRES_PORT, POSTGRES_USER, POSTGRES_PASSWORD
#
# Usage (repo root):
#   ./deploy/migrate/migrate-all.ps1
#   $env:POSTGRES_PORT = "5432"; ./deploy/migrate/migrate-all.ps1

$ErrorActionPreference = "Stop"

$Root = Resolve-Path (Join-Path $PSScriptRoot "../..")
Set-Location $Root

$PostgresHost = if ($env:POSTGRES_HOST) { $env:POSTGRES_HOST } else { "localhost" }
$PostgresPort = if ($env:POSTGRES_PORT) { $env:POSTGRES_PORT } else { "5433" }
$PostgresUser = if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { "msf" }
$PostgresPassword = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { "msf" }

function Get-ConnectionString([string]$Database) {
    "Host=$PostgresHost;Port=$PostgresPort;Database=$Database;Username=$PostgresUser;Password=$PostgresPassword"
}

function Invoke-Migrate([string]$Project, [string]$Database) {
    $cs = Get-ConnectionString $Database
    Write-Host "==> Migrating $Database ($Project)"
    dotnet ef database update --project $Project --startup-project $Project --connection $cs
    if ($LASTEXITCODE -ne 0) {
        throw "Migration failed for $Database"
    }
}

dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed" }

# Logging is Mongo-backed and has no EF migrations.
Invoke-Migrate "src/Services/Identity/MicroServiceSystem.Services.Identity.Persistence/MicroServiceSystem.Services.Identity.Persistence.csproj" "identity"
Invoke-Migrate "src/Services/User/MicroServiceSystem.Services.User.Persistence/MicroServiceSystem.Services.User.Persistence.csproj" "user"
Invoke-Migrate "src/Coordinator/Coordinator.Persistence/Coordinator.Persistence.csproj" "coordinator"
Invoke-Migrate "src/Services/Audit/MicroServiceSystem.Services.Audit.Persistence/MicroServiceSystem.Services.Audit.Persistence.csproj" "audit"
Invoke-Migrate "src/Services/File/MicroServiceSystem.Services.File.Persistence/MicroServiceSystem.Services.File.Persistence.csproj" "file"
Invoke-Migrate "src/Services/Location/MicroServiceSystem.Services.Location.Persistence/MicroServiceSystem.Services.Location.Persistence.csproj" "location"
Invoke-Migrate "src/Services/Notification/MicroServiceSystem.Services.Notification.Persistence/MicroServiceSystem.Services.Notification.Persistence.csproj" "notification"
Invoke-Migrate "src/Services/Settings/MicroServiceSystem.Services.Settings.Persistence/MicroServiceSystem.Services.Settings.Persistence.csproj" "settings"

Write-Host "All Postgres schemas are up to date."
