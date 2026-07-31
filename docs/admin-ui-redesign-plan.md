# MSF Enterprise Platform Console — UX Architecture & Implementation Roadmap

> **Document type:** Analysis + Information Architecture + Design System + Surface specs + Phased roadmap  
> **Status:** Architecture contract — **no application code in this document**  
> **Date:** 2026-07-31  
> **Scope:** Frontend UX only (`apps/admin`). Backend, APIs, contracts, CQRS, BuildingBlocks, services: **out of scope / frozen**.  
> **Related:** [admin-panel.md](./admin-panel.md), [distributed-workflows.md](./distributed-workflows.md), [README.md](../README.md), [.cursor/agents/master-architect-agent.md](../.cursor/agents/master-architect-agent.md)

---

## 0. Decision summary

| Field | Value |
|-------|--------|
| **Product identity** | **MSF Platform Console** — operating system for an Enterprise Microservice Framework (not SaaS admin, not ERP/CRM/e-commerce) |
| **Inspiration** | Azure Portal · Backstage · K8s Dashboard · Grafana · RabbitMQ Management · GitHub Actions · Datadog — **inspiration only; do not copy chrome** |
| **Foundation today** | Control Center shell already exists (sidebar, palette, hub scaffolds, real health/outbox/CRUD) |
| **Gap** | Frontend still reads as "Tabler + hubs + CRUD"; it does not yet *communicate* Clean Architecture / Saga / Outbox / BuildingBlocks power |
| **Strategy** | Elevate UX architecture in phases; bind real APIs where they exist; mark all other surfaces **UI preview — awaiting API**; **zero regression** on existing features |
| **Verdict** | **APPROVE WITH CONDITIONS** — proceed phase-by-phase; never invent fake "live" metrics; never break JWT CRUD paths |

### Conditions (non-negotiable)

1. No backend / gateway / Rabbit / Coordinator / contract changes in UI phases.
2. Do not remove Settings, Users, Tenants, Roles, Audit, Logs, Countries, Files, Notifications, Health, Outbox ops.
3. Prefer **additive routes** and **aliases/redirects** over breaking bookmarks.
4. Every surface without an API shows an explicit preview contract.
5. Each implementation phase must `npm run build` green before the next.
6. Tabler remains the CSS substrate; identity is expressed via **tokens, density, IA, and domain visualization** — not a kit swap.

---

## 1. Framework understanding (context for UX)

The console must teach and operate this mental model:

| Concept | UX implication |
|---------|----------------|
| **Database-per-service** | Never show "shared DB"; ownership appears on Service + Architecture views |
| **Gateway (YARP)** | Edge node on Platform Map; health + route prefixes |
| **Coordinator + Saga** | Dedicated Workflow Center; RegisterUser as reference orchestration |
| **Outbox / Inbox** | First-class Messaging surfaces; Identity outbox is live today |
| **Choreography** | Event Flow views; Contracts / Integration Events in Architecture |
| **BuildingBlocks** | Explorer + "Used by" — technical capability inventory |
| **Lite / full / obs profiles** | Platform Overview and Platform Map must reflect compose reality |
| **Permissions** | Nav and actions remain claim-gated |

---

## STEP 1 — Current state analysis report

### 1.1 Stack

React 19 · Vite 7 · TypeScript · Tabler Core + Icons · React Router 7 · JWT session · lazy route splitting. ApexCharts removed.

### 1.2 Current routes

| Route | Page | Nature |
|-------|------|--------|
| `/login` | LoginPage | Real auth |
| `/` | HomePage | Hybrid Overview (health/outbox/audit/logs + preview tiles) |
| `/platform` | PlatformPage | Hybrid packages + health |
| `/health` | HealthPage | Real health aggregate |
| `/services`, `/services/:id` | ServicesPage | Hybrid Service Center |
| `/messaging` | MessagingPage | Real Identity outbox + topology preview |
| `/workflows` | WorkflowsPage | Preview saga hub |
| `/observability` | ObservabilityPage | Preview + external tool links |
| `/architecture` | ArchitecturePage | Preview topology |
| `/building-blocks` | BuildingBlocksPage | Static catalog |
| `/developer` | DeveloperPage | CLI copy UX |
| `/settings` | SettingsPage | Real CRUD + ETag |
| `/users`, `/users/register`, `/users/:userId` | Users* | Real |
| `/tenants`, `/roles` | Tenants/Roles | Real (roles read-only) |
| `/audit`, `/logs` | Audit/Logs | Real (full profile) |
| `/countries`, `/files`, `/notifications` | Reference tools | Real (full) |

### 1.3 Current navigation

Eleven sidebar sections: Overview · Platform · Services · Messaging · Workflows · Observability · Identity · Configuration · Reference Data · Developer Tools · Architecture.

Ctrl+K command palette, favorites/recent (localStorage), path-segment breadcrumbs, `?` shortcuts.

**Problem:** Still partially CRUD-skewed. Health lives under Platform while Observability is separate — mental model fragments. Flat Service Center will not scale to dozens of services without search/filter/pins.

### 1.4 Current components

| Layer | Items |
|-------|--------|
| `components/ui` | PageHeader, ErrorAlert, ServiceUnavailableAlert, PaginationBar, FieldErrors |
| `components/control` | StatusBadge, HealthIndicator, MetricCard, SectionHeader, Skeleton, EmptyState, PreviewBanner, ServiceCard + tones |
| Other | UserPicker |
| Layout | AppLayout, CommandPalette, navConfig, controlCenter.css |

**Missing vs enterprise console:** PageFrame, DataTableShell, DetailDrawer, DependencyGraph / PlatformMap, Timeline, StepFlow, QueueCard, WorkflowCard, ActivityFeed, LogViewer, MetricChart shell, ToolRegistry, ForbiddenState, FilterBar, virtualized tables.

### 1.5 Current API usage (real)

Auth · Settings · Identity admin (tenants/users/roles) · Users/profiles/registration · Audit list · Logs list · Location countries · File upload · Notifications create · Ops health aggregate · Identity outbox snapshot/requeue.

**Not used by UI (clients exist):** createAuditEntry, getLog, getCountry, getSetting.

### 1.6 Strengths

- Working JWT + permission-aware nav
- Real ops foothold (health + Identity outbox)
- Control Center shell and hub scaffolds already started
- Package catalog maps lite/full/obs
- Clear preview banners on non-API surfaces
- Lazy routes; no fake SaaS sales charts

### 1.7 Weaknesses

- Dual header systems (`PageHeader` vs `SectionHeader`)
- Health shown in Overview + Platform + Health + Services
- Deep links (Seq/Jaeger/Grafana/Rabbit) duplicated
- Architecture graph is static rows, not an interactive dependency model
- Service detail is a thin panel, not a multi-tab Service Center
- Messaging is one page, not a Rabbit-grade center
- Workflows/Observability/Developer are shallow shells
- CRUD pages still feel like Bootstrap forms/tables
- Silent permission redirect to `/`
- Breadcrumbs/favorites show raw paths

### 1.8 UX problems

- First impression: "admin with extra pages," not "platform OS"
- Architect cannot *see* saga/outbox/inbox/gateway relationships in one glance
- Preview and live data sometimes sit side-by-side without visual hierarchy
- No correlation-first observability story
- No pinned services / dockable workspaces

### 1.9 Design problems

- Tabler default density and card chrome still dominate
- No documented token set as a system
- Inconsistent empty/loading patterns
- Little intentional motion; no panel resize language

### 1.10 Scalability problems

- Nav will not gracefully host dozens of future services
- Catalog metadata split across pages
- Messaging/Workflows cannot grow into sub-routes without IA redesign
- No virtualization strategy for large log/audit/outbox tables

---

## STEP 2 — Target Information Architecture

### 2.1 Product name

**MSF Platform Console** (short chrome brand may remain "MSF Control").

### 2.2 Navigation model (platform-first)

```
PLATFORM
  Overview              /
  Platform Map          /map                 ← heart of the product
  Services              /services
  Infrastructure        /infrastructure      ← redis/pg/mongo/rabbit/docker (compose-aware)

OPERATIONS
  Messaging             /messaging/*
  Workflows             /workflows/*
  Identity              /users, /tenants, /roles, /users/register

OBSERVABILITY
  Observability Hub     /observability/*
  (Audit / Logs as children; keep /audit /logs aliases)

ARCHITECTURE
  Architecture Explorer /architecture
  BuildingBlocks        /building-blocks

DEVELOPER
  Developer Center      /developer/*

REFERENCE & CONFIG
  Reference Data        /countries, /files, /notifications
  Configuration         /settings
  Preferences           (theme, density, pinned — client-only)
```

### 2.3 Route policy

| Rule | Detail |
|------|--------|
| Preserve | All existing functional routes continue to work |
| Alias | `/health` → Platform Map or Infrastructure health section |
| Expand | Messaging / Workflows / Observability / Services use **nested child routes** |
| Avoid | Unnecessary renames of `/settings`, `/users`, etc. |
| Scale | Services list supports search/filter/tags; future services appear from catalog + health |

### 2.4 Primary journeys

1. **Architect lands** → Overview → Platform Map → Service or Saga drill-down.
2. **Operator incident** → Overview critical tiles → Messaging DLQ or Workflows failed → Observability.
3. **Developer onboarding** → Architecture → BuildingBlocks → Developer Center.
4. **Tenant admin** → Identity / Settings / Reference (capabilities preserved, chrome elevated).

---

## STEP 3 — Enterprise Design System ("MSF Console DS")

Built **on Tabler**, not replacing it. Documented token layer (CSS variables).

### 3.1 Spacing scale

`4 · 8 · 12 · 16 · 24 · 32 · 48 · 64` (px). Page padding default `24`; dense mode `16`.

### 3.2 Typography

| Role | Guidance |
|------|----------|
| Display / section | Semibold hub titles |
| Body | Tabler body; 14–15px |
| Meta / IDs / routes | Monospace for service ids, trace ids, queue names, ETags |
| Nav | Compact; section labels uppercase micro |

Identity comes from structure and color semantics first — not a marketing font swap.

### 3.3 Radius & elevation

- Radius: `6` controls, `8` cards/nodes, `12` drawers
- Prefer **border + subtle surface shift** over multi-layer shadows
- No glow-heavy "AI dashboard" look

### 3.4 Semantic color system

| Token | Meaning |
|-------|---------|
| `--msf-healthy` | Ready / OK |
| `--msf-degraded` | Warning / slow / partial |
| `--msf-critical` | Down / DLQ / failed saga |
| `--msf-info` | Informational / in-progress |
| `--msf-messaging` | Broker / outbox / inbox |
| `--msf-infra` | Redis / DB / Docker |
| `--msf-workflow` | Saga / coordinator |
| `--msf-preview` | UI-only surfaces |

### 3.5 Card system

MetricCard · ServiceCard · QueueCard / WorkflowCard / EventCard · PanelCard. Avoid card-for-everything; Map heart is the graph.

### 3.6 Grid

Overview: KPI row → main (8) + activity (4). Map: full-bleed canvas + inspector. Tables: full width under PageFrame.

### 3.7 Dark / light

Dark-first; light via ThemeContext; status hues recognizable in both.

### 3.8 Interaction states

Hover: surface secondary + border accent. Active nav: primary tint. Disabled ops: tooltip "No ops API". Visible focus rings.

### 3.9 Loading quintet

Every hub/CRUD list must support: Skeleton · EmptyState · ErrorAlert · ForbiddenState · PreviewBanner.

### 3.10 Motion

Drawer slide, map node focus, timeline highlight, skeleton shimmer — `150–220ms`. No decorative noise.

### 3.11 Component hierarchy

```
tokens → primitives → composites → domain (PlatformMap, QueueCard) → PageFrame → hubs/CRUD
```

---

## STEP 4 — Platform Overview

**Route:** `/` (keep). Ban revenue/sales/growth/subscription metaphors forever.

### Live widgets (when permission + API exist)

Platform health counts · Gateway/Coordinator/Settings/Identity/User · Outbox pending + DLQ · Recent audit/logs · Profile inference (lite/full/obs).

### Preview widgets (bannered)

Running sagas · Inbox pending · Avg API latency · Queue delay · Event processing · Deployments · Docker/container counts · Redis/Mongo/Rabbit deep metrics.

### Layout

1. Health KPI strip  
2. Infra status row  
3. Messaging + Workflow snapshot  
4. Activity feed  
5. Quick actions  

Feel **real-time ready**: refresh control + last-updated timestamp.

---

## STEP 5 — Platform Map (product heart)

**Route:** `/map` (new).

**Split:** `/map` = runtime topology (health-aware). `/architecture` = design-time bounded contexts, contracts, BuildingBlocks, DB ownership.

### Requirements

Interactive React visualization (SVG/Canvas/CSS — **no static image**). Nodes: Gateway → Services → RabbitMQ → Redis → Databases → Coordinator → Notification → Audit → Logging → External tools.

Each node: status color · click → inspector · dependencies · version/label · health · metrics placeholders · deep links.

### Data

Static platform topology catalog + live health overlay. No backend redesign.

---

## STEP 6 — Service Center

**Routes:** `/services` · `/services/:serviceId` with **tabs**.

Tabs (UI architecture; backend optional): Overview · Health · Dependencies · Database · Redis · RabbitMQ · Outbox · Inbox · Metrics · Tracing · Logs · Configuration · Environment · Docker · OpenAPI · Version · Deployment · Health Timeline.

Restart: disabled + tooltip until ops API. List: filter, search, pin; virtualize when needed. Preserve catalog + health + OpenAPI.

---

## STEP 7 — Messaging Center

Nested under `/messaging`:

| Child | Live today? |
|-------|-------------|
| Overview KPIs | Partial (Identity) |
| Queues / Exchanges / Bindings | Preview |
| Publishers / Consumers | Preview |
| Dead Letters + requeue | **Live (Identity)** |
| Retries / Replay / Inspect / Timeline | Preview |
| Outbox | Identity live; others preview |
| Inbox / Event Flow | Preview |

Reusable: QueueCard, EventCard, DetailDrawer, DataTableShell, Timeline.

---

## STEP 8 — Workflow Center

`/workflows` · `/workflows/:sagaId`.

Boards: Running · Completed · Failed · Compensated · Waiting · Retrying.

RegisterUser StepFlow: Identity → User → Notification → Audit → Completed (+ compensation Identity.Disable). Timeline + lease/recovery education copy. Link to registration.

---

## STEP 9 — Observability Center

Do **not** replace Grafana/Seq/Jaeger. Nested hub: Metrics · Tracing · Logs · Audit · Errors · Performance · OTel · Prometheus · Correlation Explorer (preview until BFF). Keep `/audit` `/logs` aliases.

---

## STEP 10 — Architecture Explorer

Design-time: Bounded contexts · Dependencies · BuildingBlocks · Integration events / Contracts · Event flow · Database ownership. Cross-link Map/Services/Blocks.

---

## STEP 11 — Developer Center

Wizard UX only (copyable CLI): Create Service · Aggregate · CRUD · Event · Saga · BuildingBlock · Templates. No remote codegen.

---

## STEP 12 — BuildingBlock Explorer

Inventory aligned with repo: Authentication · Authorization · Persistence · Messaging · Caching · Storage · Logging · Localization · OpenTelemetry · HealthChecks · Saga · Idempotency · MultiTenancy · Resilience · Application · ServiceDefaults · SharedKernel · Contracts.

Each: Purpose · Dependencies · Used By · Version · Docs.

---

## STEP 13 — Global UX

Extend palette beyond routes · shortcuts · quick actions · favorites/pins with labels · recent activity · toasts · resolved breadcrumbs · responsive sidebar · resizable inspectors (late) · client preferences (theme/density/landing).

---

## STEP 14 — Component catalog

**Evolve:** StatusBadge, HealthIndicator, MetricCard, ServiceCard, Skeleton, EmptyState, PreviewBanner, UserPicker, PaginationBar, alerts, CommandPalette.

**New:** PageFrame · DataTableShell · DetailDrawer · FilterBar · Timeline · StepFlow · ActivityFeed · PlatformMap · DependencyNode · DependencyGraph · QueueCard · WorkflowCard · EventCard · LogViewer · MetricChart shell · HealthBadge · ToolRegistry · ForbiddenState · CopyCommandBlock · HealthSummaryStrip.

**Rule:** No duplicated status/KPI/table/drawer/tool markup.

---

## STEP 15 — Performance

Keep lazy routes; split nested hub chunks · virtualize large tables · memoize graph only when profiled · avoid heavy charts until metrics BFF · trim dead assets · tree-shakeable catalog modules.

---

## STEP 16 — Preserve functionality

Must keep: Login · Settings ETag · Users/disable/profile/register · Tenants · Roles read · Audit/Logs/Countries/Files/Notifications · Health aggregate · Identity outbox requeue · Preview banners.

Regression every phase: login → overview → settings upsert → users list → outbox → health.

---

## STEP 17 — Phased roadmap

| Phase | Focus | Exit criteria |
|-------|--------|----------------|
| **1** | Design System (tokens, PageFrame, ForbiddenState, ToolRegistry, CRUD skeletons) | Visual consistency; build green |
| **2** | Platform-first nav; `/map` stub; `/health` redirect strategy | Nav reads as platform OS; old routes work |
| **3** | Overview v2 (KPI, infra, activity, quick actions) | Architect reads health in &lt;1 min |
| **4** | Interactive Platform Map + inspector + health overlay | Map is the heart |
| **5** | Service Center tabs + FilterBar + pins | Control-plane service page |
| **6** | Messaging nested routes; live DLQ elevated; preview modules | Rabbit-grade IA, honest preview |
| **7** | Workflow StepFlow + Timeline + boards | Orchestration teachable |
| **8** | Observability hub; embed Audit/Logs; Correlation shell | Grafana not replaced; journey clear |
| **9** | Architecture vs Map split; BuildingBlocks detail; Developer wizards | Onboarding path complete |
| **10** | Polish: resizable panels, virtual tables, docs, a11y, cleanup | Enterprise cohesion — **done (partial):** HubTabs a11y, docs sync, dead HealthPage removed; Map resize + CRUD shells; virtual tables deferred |
| **11** | Backend-gated fill (separate epic, Architect-approved APIs) | **DECIDED:** [phase-11-ops-api-epic.md](phase-11-ops-api-epic.md) — C1–C4 conditions; C5/C6 REJECT; P0 unlocked |

**Never invent fake live metrics. Never leave the app broken between phases.**

---

## Preview contract

```
IF no API for this panel:
  show PreviewBanner
  use labeled sample or empty+explanation
  disable mutating actions
ELSE
  bind real client
  never silently fall back to fake numbers
```

---

## Success criteria

1. Architect explains Gateway → Services → Broker → Saga → Observability from UI in minutes.
2. Console no longer feels like Tabler demo CRUD.
3. Platform Map is interactive and health-aware.
4. Centers are modular and nested.
5. Zero functional regression on JWT features.
6. No backend redesign.
7. Design system eliminates duplicated UI.
8. Performance strategy keeps console snappy.
9. Docs accurately reflect live vs preview.

---

## Non-goals

- Replacing Tabler · Copying Azure/Backstage chrome · Implementing Rabbit/Coordinator/Prometheus backends in SPA · Helm/K8s operator product · Fake revenue dashboards

---

## Closing

The backend already embodies an enterprise microservice framework. The console must make that architecture **operable and legible**. Path: Overview for pulse, **Platform Map for truth**, Centers for depth, Design System for coherence, Preview honesty for trust — an **MSF Platform Console**, not another admin theme.
