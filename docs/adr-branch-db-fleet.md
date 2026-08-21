# ADR: Branch database fleet (şube başına ayrı DB)

**Status:** Accepted (Phase 1)  
**Date:** 2026-08-21  
**Architect:** APPROVE WITH CONDITIONS (Master Architect)

## Context

Product requires physical isolation: branch (şube) A must never open branch B’s database. Scale target is **1000+** branches. Today the platform uses a **shared database per microservice** with `TenantId` EF filters (logical isolation only).

## Decision

1. **Şube = existing Identity `Tenant`.** JWT claim `tenant_id` remains the routing key.
2. **Isolation unit:** one PostgreSQL **database per (şube × data-service)**. Host/cluster sharing is allowed; database name sharing across tenants is forbidden (R01/R02).
3. **Control plane (shared Identity Postgres):** catalog only — `PostgresCluster` + `TenantDatabaseBinding`. No User OLTP rows in Identity.
4. **Phase 1 data plane:** **User** service only (`ServiceKey` allow-list: `"user"`). Identity/Gateway/Admin/Coordinator stay shared.
5. **Hosting model:** N Postgres clusters in a fleet. Phase 1 Compose = one Postgres container hosting many databases (not 1000 instances / Supabase projects).
6. **Secrets:** bindings store `SecretRef` + `Username` + host metadata. Admin and internal resolve APIs **never** return passwords or full connection strings. User materializes Npgsql strings from binding DTO + `configuration[SecretRef]`.
7. **Permissions:** `identity.tenant-databases.read` / `identity.tenant-databases.write` (not only tenants read/write).
8. **Deactivate tenant:** bindings → `Disabled` + Outbox `TenantDatabaseAccessChanged`; User Inbox drops cached `NpgsqlDataSource`.
9. **Non-goals:** Supabase Auth/RLS as tenancy; MCP in runtime; Identity users in per-şube DB; multi-cluster admin APIs in Phase 1.

## Compose local SoT (Phase 1)

One Postgres container hosts many databases (1000+ simulation without 1000 containers):

1. Identity seeds cluster `local` + Ready binding for demo tenant → database `user`.
2. Admin **Provision User DB** on a new şube → `CREATE DATABASE user_<slug>` + User `ensure-migrated`.
3. User pods: `Persistence__Postgres__Mode=TenantScoped` + `Services__Identity__BaseUrl`.
4. Required env: `Persistence__Postgres__AdminConnection`, `Persistence__Postgres__AppPassword`.

At scale, add PgBouncer/RDS Proxy when connection churn is measured — not required for local Compose.

```
User request → JWT tenant_id → ICurrentTenant
  → GET internal …/binding (Ready only)
  → Host/Port/Database/Username + config[SecretRef]
  → LRU NpgsqlDataSource cache (tenantId|serviceKey)
```

Provisioner uses cluster `AdminSecretRef` for `CREATE DATABASE` and Identity-mediated `SELECT 1` health.

## Consequences

- EF `TenantId` filters remain as a second safety belt.
- Migrations fan out per binding (batched); deploy must not block on 1000 sequential migrates.
- At scale, put **PgBouncer / RDS Proxy** in front of clusters when connection churn is measured (not required for Phase 1 Compose).
- Later services (Location, File, …) reuse the same binding + resolver pattern.

## API surface (approved)

| Method | Route | Auth |
|--------|-------|------|
| GET | `/tenants/{tenantId}/databases` | `identity.tenant-databases.read` |
| POST | `/tenants/{tenantId}/databases/{serviceKey}/provision` | `identity.tenant-databases.write` |
| POST | `/tenants/{tenantId}/databases/{serviceKey}/retry` | `identity.tenant-databases.write` |
| POST | `/tenants/{tenantId}/databases/{serviceKey}/health` | `identity.tenant-databases.write` |
| GET | `/tenants/{tenantId}/databases/{serviceKey}/binding` | Internal API key |

Activation remains `identity.tenants.write` and must disable bindings + publish access-changed event.
