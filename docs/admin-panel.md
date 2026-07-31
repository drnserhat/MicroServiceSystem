# Admin panel — Platform Control Center

Operator console for MicroServiceSystem: React + Vite + TypeScript + [Tabler](https://preview.tabler.io/) dark theme, talking to the API gateway with JWT.

**UI:** http://localhost:5173 (Docker lite)  
**API:** http://localhost:8080 (or same-origin via admin nginx / Vite proxy)  
**Dev login:** `admin@dev.local` / `DevAdmin!Pass1`  
**Demo tenant:** `11111111-1111-1111-1111-111111111111`

After Identity permission changes, **log out and log in again** so the JWT picks up new `permission` claims (roles sync on login).

---

## Current status (2026-07)

| Area | Status | Notes |
|------|--------|--------|
| Auth (login / refresh / logout) | Done | Gateway JWT + `X-Tenant-Id` |
| Control Center shell | Done | Collapsible sidebar, breadcrumbs, Ctrl+K palette, favorites/recent |
| Design system (Phase 1) | Done | MSF tokens, `PageFrame`, `ForbiddenState`, `ToolRegistry`, table skeletons |
| Platform-first nav (Phase 2) | Done | Platform / Operations / Observability / Architecture / Developer / Reference & Config; `/map` stub; `/health` → `/map` |
| Platform Overview v2 (Phase 3) | Done | KPI strip, infra row, messaging/workflow snapshot, activity feed, quick actions, refresh |
| Platform Map (Phase 4) | Done | Interactive SVG graph + inspector; health overlay; `/health` → `/map` |
| Service Center (Phase 5) | Done | Filters, pins, tabbed detail (overview→timeline); Identity outbox live on Outbox tab |
| Messaging Center (Phase 6) | Done | Nested routes; QueueCard/EventCard/DetailDrawer; live DLQ inspect+requeue; Replay/Timeline |
| Workflow Center (Phase 7) | Done | Live RegisterUser saga list/detail (`ops.saga.read`); definitions educational |
| Observability Center (Phase 8) | Done | Nested hub; live Audit/Logs embed; Grafana/Seq/Jaeger not replaced |
| Architecture / Blocks / Dev (Phase 9) | Done | Design-time explorer vs Map; full BuildingBlocks; wizard routes |
| Polish (Phase 10) | Done | HubTabs a11y; Map resizable inspector; Packages DS alignment; CRUD DataTableShell |
| Phase 11 UI slots | **P0+P1 shipped** | Outbox/pending + saga read + inbox counts — [phase-11-ops-api-epic.md](phase-11-ops-api-epic.md); C5/C6 REJECT |
| Platform Overview (`/`) | Done | Real health / outbox / audit / logs; no fake ApexCharts |
| Settings CRUD + ETag | Done | Lite stack |
| Users directory / register / profile | Done | No raw GUID entry — pick from list / UserPicker |
| Tenants list / create / activate | Done | JWT admin APIs |
| Roles & permissions (read) | Done | Catalog view |
| Audit / Logs / Countries / Files / Notify | Done | Need Docker **profile `full`** for backends |
| Service health aggregate | Done | Gateway `GET /ops/api/v1/health/services` |
| Messaging Center | Done | Live per-service outbox/DLQ + inbox counts; topology **UI preview** |
| Service Center | Done | Catalog + live health; CPU/secrets/restart = preview |
| Workflow / Architecture / BuildingBlocks / Developer | Done | Live saga ops; Architecture/Blocks/Dev remain design-time |
| User/role assignment UI | Not started | Roles are read-only in admin today |
| Outbox ops for every service | Done (P0) | Thin per-Api controllers; Gateway catch-all |
| Helm/K8s operator surface | Not started | Compose-oriented today |

Surfaces without APIs show a clear **“UI preview — awaiting API”** banner. Backend contracts are unchanged.

---

## Information architecture

```
PLATFORM
  Overview              /
  Platform Map          /map              (runtime topology; /health → here)
  Packages              /platform
  Services              /services[/:id]

OPERATIONS
  Messaging             /messaging/*      nested: queues, DLQ, outbox, …
  Workflows             /workflows/*      nested: boards, definitions, :sagaId
  Identity              /users, /tenants, /roles, /users/register

OBSERVABILITY
  Hub                   /observability/*  metrics→correlation; embeds audit/logs
  Audit / Logs aliases  /audit, /logs     standalone (same browsers)

ARCHITECTURE
  Explorer              /architecture/*   design-time (contexts→contracts)
  BuildingBlocks        /building-blocks[/:id]

DEVELOPER
  Developer Center      /developer/*      CLI wizards (copy-only)

REFERENCE & CONFIG
  Countries / Files / Notifications / Settings
```

| Section | Routes | Permissions (examples) |
|---------|--------|------------------------|
| Platform | `/`, `/map`, `/platform`, `/services` | `ops.health.read` for map/packages/services |
| Operations | `/messaging`, `/workflows`, `/users`, … | `ops.outbox.*`, `identity.*` |
| Observability | `/observability`, `/audit`, `/logs` | `audit.*`, `logging.logs.read` |
| Architecture | `/architecture`, `/building-blocks` | none |
| Developer | `/developer` | none |
| Reference & Config | `/countries`, `/files`, `/notifications`, `/settings` | location / file / notification / settings |

**Aliases:** `/health` redirects to `/map`.  
**Shortcuts:** `Ctrl+K` command palette · `?` shortcuts help · ★ favorites (labels via nav resolver).

---

## Stack vs features

**Lite (default compose):** postgres, redis, rabbitmq, identity, user, coordinator, settings, gateway, **admin**.

**Add-ons (`--profile full`):** audit, logging, location, file, notification, mongodb.

**Observability (`--profile obs` or with full):** Seq, Jaeger, Prometheus, Grafana.

The **Services & packages** page (`/platform`) and **Service Center** (`/services`) list each package, compose profile, gateway prefix, admin deep-link, and live health when a gateway cluster exists.

```bash
# enable add-ons + obs
docker compose -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  -f deploy/docker/docker-compose.resources.yml \
  -f deploy/docker/docker-compose.observability.yml \
  --profile full --profile obs up -d --build
```

---

## Notable admin APIs (gateway)

| Capability | Path |
|------------|------|
| Login / refresh | `POST /identity/api/v1/auth/login\|refresh` |
| Settings | `/settings/api/v1/settings` (+ `If-Match`) |
| Register user | `POST /registration` |
| Profile | `/user/api/v1/users/profiles/{id}` |
| Tenants (JWT admin) | `GET /identity/api/v1/tenants`, `POST .../tenants/admin`, `POST .../activation` |
| Users directory | `GET /identity/api/v1/users`, `POST .../users/{id}/disable` |
| Roles | `GET /identity/api/v1/roles` |
| Health aggregate | `GET /ops/api/v1/health/services` |
| Outbox snapshot / requeue | `/{service}/api/v1/ops/outbox` (identity, user, settings, coordinator, audit, notification, file, location) |

Phase 11: [phase-11-ops-api-epic.md](phase-11-ops-api-epic.md) — **Architect DECIDED**; P0 outbox + P1 saga/inbox shipped; C5/C6 rejected.

New Admin JWT permissions (in `FrameworkPermissions.AdminDefaults`):  
`identity.tenants.read|write`, `identity.users.read|disable`, `identity.roles.read`, `ops.health.read`, `ops.outbox.read|write`.

---

## Rebuild admin UI after changes

```bash
docker compose -p microsystem \
  -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  -f deploy/docker/docker-compose.resources.yml \
  up -d --build --force-recreate --no-deps admin
```

Hard-refresh the browser (`Ctrl+F5`). For Identity API / permission changes, also recreate `identity` (and re-login).

---

## Source layout

```
apps/admin/
  src/api/           Gateway clients (auth, settings, identityAdmin, ops, …)
  src/auth/          Session, JWT permission decode, RequirePermission
  src/layout/        Control Center shell, navConfig, CommandPalette
  src/components/control/  StatusBadge, MetricCard, skeletons, …
  src/pages/         Feature screens + hubs (services, workflows, …)
  src/platform/      Package catalog (core / addon / observability)
```
