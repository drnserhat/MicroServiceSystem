#!/usr/bin/env bash
# Applies EF Core migrations for every Postgres-backed service.
# Production apps keep ApplyMigrationsOnStartup=false; this script is the job the runtime comment refers to.
#
# Required env (or defaults for local compose on localhost:5433):
#   POSTGRES_HOST, POSTGRES_PORT, POSTGRES_USER, POSTGRES_PASSWORD
#
# Usage (repo root):
#   ./deploy/migrate/migrate-all.sh
#   POSTGRES_HOST=postgres POSTGRES_PORT=5432 ./deploy/migrate/migrate-all.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

POSTGRES_HOST="${POSTGRES_HOST:-localhost}"
POSTGRES_PORT="${POSTGRES_PORT:-5433}"
POSTGRES_USER="${POSTGRES_USER:-msf}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-msf}"

connection_string() {
  local database="$1"
  printf 'Host=%s;Port=%s;Database=%s;Username=%s;Password=%s' \
    "$POSTGRES_HOST" "$POSTGRES_PORT" "$database" "$POSTGRES_USER" "$POSTGRES_PASSWORD"
}

migrate() {
  local project="$1"
  local database="$2"
  local cs
  cs="$(connection_string "$database")"

  echo "==> Migrating $database ($project)"
  dotnet ef database update \
    --project "$project" \
    --startup-project "$project" \
    --connection "$cs"
}

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK is required" >&2
  exit 1
fi

dotnet tool restore

# Logging is Mongo-backed and has no EF migrations.
migrate "src/Services/Identity/MicroServiceSystem.Services.Identity.Persistence/MicroServiceSystem.Services.Identity.Persistence.csproj" "identity"
migrate "src/Services/User/MicroServiceSystem.Services.User.Persistence/MicroServiceSystem.Services.User.Persistence.csproj" "user"
migrate "src/Coordinator/Coordinator.Persistence/Coordinator.Persistence.csproj" "coordinator"
migrate "src/Services/Audit/MicroServiceSystem.Services.Audit.Persistence/MicroServiceSystem.Services.Audit.Persistence.csproj" "audit"
migrate "src/Services/File/MicroServiceSystem.Services.File.Persistence/MicroServiceSystem.Services.File.Persistence.csproj" "file"
migrate "src/Services/Location/MicroServiceSystem.Services.Location.Persistence/MicroServiceSystem.Services.Location.Persistence.csproj" "location"
migrate "src/Services/Notification/MicroServiceSystem.Services.Notification.Persistence/MicroServiceSystem.Services.Notification.Persistence.csproj" "notification"
migrate "src/Services/Settings/MicroServiceSystem.Services.Settings.Persistence/MicroServiceSystem.Services.Settings.Persistence.csproj" "settings"

echo "All Postgres schemas are up to date."
