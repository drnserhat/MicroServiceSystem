# Phase 11 — Ops API Epic (Architect briefing)

**Status:** **DECIDED** — Master Architect **APPROVE WITH CONDITIONS** (C1–C4); **REJECT** (C5 BFF, C6 in-console Prometheus)  
**Decision date:** 2026-07-31  
**Requester:** Admin Platform Console (UI Phases 1–10 complete)  
**Constraint:** Implement only against the locked decision below. Admin must not invent fake live metrics.

**Related:**
- [admin-ui-redesign-plan.md](admin-ui-redesign-plan.md) Phase 11
- [admin-panel.md](admin-panel.md)
- Live client today: `apps/admin/src/api/ops.ts`
- Reference implementation: `Identity` `OutboxOpsController` (`GET/POST …/ops/outbox`)

---

## 0. Master Architect Decision (LOCKED)

| # | Capability | Karar | Öncelik |
|---|------------|--------|---------|
| **C1** | Per-service outbox snapshot + DLQ + requeue | **APPROVE WITH CONDITIONS** | P0 |
| **C2** | Pending outbox row list (read) | **APPROVE WITH CONDITIONS** | P0 (with C1) |
| **C3** | Coordinator saga list/get (read-only) | **APPROVE WITH CONDITIONS** | P1 |
| **C4** | Inbox counts only | **APPROVE WITH CONDITIONS** | P1 |
| **C5** | Correlation join BFF | **REJECT** (this quarter) | — |
| **C6** | In-console Prometheus | **REJECT** | — |

**Epic dışı (veto):** Rabbit Management proxy, remote restart, secret/env dump, remote codegen, Gateway god-BFF.

### Locked answers

| # | Question | Decision |
|---|----------|----------|
| 1 | Shared MVC controller in BuildingBlocks? | **NO** — convention + thin per-host controller; port/DTO helpers OK in BuildingBlocks |
| 2 | Pending list payload in P0? | **NO** — metadata only |
| 3 | Saga read permission? | **`ops.saga.read`** (new); no `ops.saga.write` in P1 |
| 4 | TenantIndependent on ops surfaces? | **YES** for `ops/outbox`, `ops/inbox/summary`, `ops/sagas` only |
| 5 | C5 BFF this quarter? | **REJECT** — logs + Seq/Jaeger deep-links |

### P0 implementation gate

P0 may start: replicate Identity outbox ops (convention) on User / Settings / Coordinator (+ full-profile services); add pending metadata list; gateway routes; admin multi-service bind. **No shared OutboxOpsController in BuildingBlocks. No payload field.**

---

## 1. Karar talebi

Admin SPA hub’ları (Messaging, Workflows, Observability charts, multi-service Outbox) **UI preview** durumunda. Preview banner’ları kaldırmak için aşağıdaki capability’lerin Architect onayı ve ardından implementasyonu isteniyor.

| # | Capability | Admin yüzeyleri | Bugün |
|---|------------|-----------------|-------|
| C1 | Per-service outbox snapshot + DLQ + requeue | `/messaging/*`, `/services/:id` Outbox, Overview KPIs | Yalnızca Identity |
| C2 | Pending outbox row list (read) | `/messaging/outbox` | Yok (yalnızca count) |
| C3 | Coordinator saga list/get (read-only) | `/workflows/*` | Preview katalog |
| C4 | Inbox counts per service | `/messaging/inbox` | Preview |
| C5 | Correlation join BFF | `/observability/correlation` | Logs filter canlı; join yok |
| C6 | Metrics in-console | Obs MetricChartShell | Grafana deep-link; shell boş |

---

## 2. Önerilen karar özeti (talep edilen)

| Capability | Talep | Gerekçe |
|------------|-------|---------|
| **C1** | **APPROVE** (P0) | Identity pattern zaten var; her BC kendi `outbox_messages` tablosuna sahip — ownership bozulmaz |
| **C2** | **APPROVE** (P0, C1 ile) | Operatör pending backlog’u sayının ötesinde görmeli; payload dump şart değil |
| **C3** | **APPROVE** (P1) | RegisterUser öğretilebilirliği; read-only ops, domain mutate yok |
| **C4** | **APPROVE WITH CONDITIONS** (P1) | Yalnızca count/age aggregate; mesaj gövdesi/inbox key dump yok |
| **C5** | **DEFER / REJECT as BFF** (P2) | Logs + Seq/Jaeger deep-link yeterli; join BFF erken optimizasyon |
| **C6** | **REJECT in-console Prometheus** (P2) | Grafana/Prometheus kalsın; admin chart shell preview kalsın |

**Epic dışı (veto beklenen):** Rabbit Management proxy, remote service restart, secret/env dump, remote codegen, Helm/K8s operator UI.

---

## 3. Mimari tercih (varsayılan)

```
Admin SPA
  → API Gateway (YARP + JWT + permission)
    → {service}/api/v1/ops/outbox          (per BC — Identity pattern)
    → coordinator/api/v1/ops/sagas         (read-only — proposed)
    → {service}/api/v1/ops/inbox/summary   (counts — proposed)
  ↛ Gateway “god” BFF that reads every service DB
  ↛ Admin → Rabbit Management HTTP as system of record
```

### Zorunlu ilkeler

1. **Database-per-context** — Outbox/inbox sorguları o servisin Persistence katmanında kalır.
2. **Identity pattern replication** — `OutboxOpsController` şekli BuildingBlocks veya paylaşılan ops convention ile çoğaltılır; her servis kendi route’unu expose eder.
3. **Gateway** — Mevcut cluster route’larına `…/ops/outbox` eklenir; yeni aggregate ops host açılmaz (P0 için).
4. **Permissions** — Mevcut `ops.outbox.read` / `ops.outbox.write` / `ops.health.read` yeniden kullanılır veya Architect `ops.saga.read` / `ops.inbox.read` ekler.
5. **Tenant** — Identity outbox bugün `[TenantIndependent]`; yeni endpoint’ler aynı güvenlik modeline uymalı (Architect netleştirir).
6. **Observability** — Seq / Jaeger / Grafana / Prometheus **dış tool** kalır; admin deep-link + live Logging/Audit.

### Referans sözleşme (canlı — Identity)

- `GET /identity/api/v1/ops/outbox?take=50` → summary (`service`, `pendingCount`, `deadLetterCount`) + dead-letter rows  
- `POST /identity/api/v1/ops/outbox/{messageId}/requeue`  
- Permissions: `ops.outbox.read` | `ops.outbox.write`

P0 hedef: aynı shape’in `user`, `settings`, `coordinator`, ve full-profile servislerde (audit, notification, …) expose edilmesi; admin’in servis seçerek aynı client’ı kullanması.

---

## 4. Öncelik ve admin binding

| Öncelik | Capability | Admin slot’lar (preview kalkar) | Not |
|---------|------------|----------------------------------|-----|
| **P0** | C1 per-service outbox | Messaging Overview/Outbox/DLQ; Service Center Outbox tab; Home messaging KPI | Multi-service tabs |
| **P0** | C2 pending list | Messaging Outbox | `take` clamp; no full payload unless Architect onaylar |
| **P1** | C3 saga read | Workflow boards + `/workflows/:sagaId` | RegisterUser first; mutate/compensate API yok |
| **P1** | C4 inbox summary | Messaging Inbox | Counts only |
| **P2** | C5 correlation BFF | Correlation explorer | Optional; logs query param zaten var |
| **P2** | C6 metrics | Obs charts | Keep Grafana; no fake series |

---

## 5. Etkilenen Bounded Context / servisler

| Servis / alan | Etki |
|---------------|------|
| **Identity** | Referans; değişiklik minimal (belki pending list) |
| **User, Settings, Coordinator** | Lite: outbox ops controller + gateway route |
| **Audit, Notification, File, Location** | Full profile: aynı ops surface |
| **BuildingBlocks.Messaging / Persistence** | Paylaşılan ops helper / controller convention (Architect onayı ile) |
| **API Gateway** | YARP route/cluster path expose |
| **Admin SPA** | `ops.ts` multi-service client; preview banner kaldırma DoD’ye bağlı |
| **SharedKernel permissions** | Yeni permission sabitleri (saga/inbox) gerekirse |

---

## 6. Riskler ve mitigasyon

| Risk | Mitigasyon |
|------|------------|
| Ops endpoint’leri domain API’yi şişirir | Ayrı `ops/` route prefix; read-heavy; write yalnız requeue |
| Cross-DB join BFF | Yapma (C5 defer); deep-link |
| Payload / PII leak in outbox inspect | P0’da event name + meta; body ayrı permission veya yok |
| Saga state leak / race | Read-only; no force-complete from admin in P1 |
| Permission sprawl | Prefer existing `ops.outbox.*`; add narrowly |
| Fake metrics temptation | Preview contract unchanged until live bind |

---

## 7. Delegation planı (Architect onayından sonra)

| Sıra | Agent / rol | Görev |
|------|-------------|-------|
| 1 | **Master Architect** | Bu epic’e karar; permission ve tenant modelini kilitle |
| 2 | **backend-agent** (P0) | Outbox ops convention + User/Settings/Coordinator (+ full) controllers; pending list |
| 3 | **gateway** | Route’lar / cluster path |
| 4 | **admin (UI)** | `ops.ts` multi-service; Messaging/Services/Home bind; banner kaldırma |
| 5 | **backend-agent** (P1) | Coordinator saga read API; inbox summary |
| 6 | **admin (UI)** | Workflows + Inbox bind |
| 7 | **testing-agent** | Permission, requeue, empty/error paths |
| 8 | **review-agent** | Contract + Clean Architecture sınırı |

**Kod bu epic dokümanında yok.** Implementasyon ayrı plan/PR’lar.

---

## 8. Definition of Done (capability başına)

### C1 / C2 (P0)

- [x] Lite services: Identity-shaped outbox snapshot + DLQ + requeue
- [x] Pending list **metadata-only** (no payload)
- [x] Gateway admin access; `ops.outbox.read|write`; TenantIndependent on ops only
- [x] **No** shared MVC OutboxOpsController in BuildingBlocks
- [x] Admin Messaging + Service Outbox preview cleared for covered services
- [ ] Home messaging KPI uses real data only _(Identity summary retained on Overview)_
- [ ] Identity outbox regression yeşil _(verify after deploy)_

### C3 (P1)

- [x] Coordinator saga list/get read-only; permission **`ops.saga.read`**
- [x] No mutate/compensate from admin
- [x] Workflow boards bind live state

### C4 (P1)

- [x] Inbox summary counts-only; permission **`ops.inbox.read`**
- [x] No message/key dump
- [x] Messaging Inbox banner cleared

> **Note:** After Identity permission seed includes the new codes, admin users must **re-login** so JWT claims pick up `ops.saga.read` / `ops.inbox.read`.

### C5 / C6 (P2)

- [x] **REJECT** recorded — no BFF / no in-console Prometheus this quarter
- [ ] Correlation: logs + Seq/Jaeger deep-link only
- [ ] Metrics: Grafana deep-link; chart shell stays PreviewBanner

---

## 9. Preview contract (değişmez)

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

## 10. Architect questions — answered

1. Shared BuildingBlocks controller? → **Convention + thin per-Api controller** (shared MVC controller rejected).
2. Pending payload in P0? → **No.**
3. Saga permission? → **`ops.saga.read`.**
4. TenantIndependent on ops? → **Yes** (ops surfaces only).
5. C5 BFF this quarter? → **Reject.**

---

## Closing

UI Platform Console is ready. Phase 11 value is honest live data. **Decision is locked:** P0 outbox convention rollout may proceed; C5/C6 and shared outbox MVC controller remain vetoed.