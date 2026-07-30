# MicroServiceSystem Admin

React + Vite + TypeScript SPA using [Tabler](https://github.com/tabler/tabler) ([preview](https://preview.tabler.io/)).

**Status, menu map, and roadmap:** see [docs/admin-panel.md](../../docs/admin-panel.md).

## Docker (default lite stack)

```bash
docker compose -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  -f deploy/docker/docker-compose.resources.yml \
  up -d --build
```

Open **http://localhost:5173**.

| Field | Value |
|-------|--------|
| Email | `admin@dev.local` |
| Password | `DevAdmin!Pass1` |
| Tenant | `11111111-1111-1111-1111-111111111111` |

If login returns 401 after upgrading, rebuild Identity (`--build`) so tenant catalog migrations + admin seed apply. After permission changes, **log in again**.

### Rebuild only the admin image

```bash
docker compose -p microsystem \
  -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  -f deploy/docker/docker-compose.resources.yml \
  up -d --build --force-recreate --no-deps admin
```

Then hard-refresh (`Ctrl+F5`).

## What you can do today

- **Platform Overview** — live health, outbox, recent audit/logs (permission-gated); no demo sales charts  
- **Service Center / Packages** — microservice inventory + OpenAPI deep links; preview for CPU/secrets/restart  
- **Messaging / Workflows / Observability** — outbox ops + UI hubs for Rabbit topology, RegisterUser saga, OTel tools  
- **Architecture / BuildingBlocks / Developer** — topology map, building-block catalog, copyable `dotnet new` CLI  
- **Settings** — tenant key/value CRUD with concurrency (`If-Match`)  
- **Users** — search directory, open profile (no GUID typing), disable with reason, register via saga  
- **Tenants / Roles** — tenant catalog + role permission catalog  
- **Full-profile tools** — Audit, Logs, Countries, File upload, Notifications (need `--profile full`)

Shell: sidebar IA, Ctrl+K jump-to, favorites/recent in localStorage. Notifications and similar flows use a **user picker**, not raw IDs.

## Local Vite

```bash
cd apps/admin
npm install
npm run dev
```

Proxies `/identity`, `/settings`, `/user`, `/ops`, `/registration`, … to `http://localhost:8080`. Optional: `VITE_API_BASE_URL=http://localhost:8080`.

## Stack notes

| Compose | Includes |
|---------|----------|
| default (lite) | gateway, identity, user, coordinator, settings, admin + infra |
| `--profile full` | + audit, logging, location, file, notification, mongo |
| `--profile obs` | + Seq, Jaeger, Prometheus, Grafana |
