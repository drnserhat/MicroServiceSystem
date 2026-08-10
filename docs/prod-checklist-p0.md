# P0 production checklist — MicroServiceSystem

Go / no-go gate before real customer traffic. Companion docs:

- [Helm chart README](../deploy/helm/microservice-system/README.md)
- [values-production.example.yaml](../deploy/helm/microservice-system/values-production.example.yaml)
- [External Secrets sample](../deploy/helm/microservice-system/examples/external-secret.yaml)
- [OWASP baseline](../deploy/security/owasp-baseline.md)
- [Admin panel](admin-panel.md)

**Minimum bar for a controlled first prod:** sections **1 + 2 + 3 + 6** all green. Sections **4–5** should be green before calling it a durable production; without them treat the environment as internal / staging-prod only.

### Operator scripts (repo)

| Script | Purpose |
|--------|---------|
| [`deploy/scripts/validate-prod-values.sh`](../deploy/scripts/validate-prod-values.sh) / [`.ps1`](../deploy/scripts/validate-prod-values.ps1) | Fail if values still contain `REQUIRED_` / `change-me` / `latest` |
| [`deploy/scripts/smoke-prod.sh`](../deploy/scripts/smoke-prod.sh) / [`.ps1`](../deploy/scripts/smoke-prod.ps1) | Post-deploy health (+ optional login / 401/200 roles) |

```bash
cp deploy/helm/microservice-system/values-production.example.yaml values-production.local.yaml
# fill secrets… (file is gitignored)
./deploy/scripts/validate-prod-values.sh values-production.local.yaml
# Windows: .\deploy\scripts\validate-prod-values.ps1 values-production.local.yaml

helm upgrade --install msf ./deploy/helm/microservice-system \
  -n msf --create-namespace -f values-production.local.yaml

MSF_BASE_URL=https://admin.example.com \
MSF_EMAIL=... MSF_PASSWORD=... MSF_TENANT_ID=... \
  ./deploy/scripts/smoke-prod.sh
# Windows: $env:MSF_BASE_URL=...; .\deploy\scripts\smoke-prod.ps1
```

P1 (later): HPA, NetworkPolicy / mTLS, multi-AZ DR automation, chaos, pen-test report, product domain services.

---

## 1. Secrets & identity

- [ ] `jwtSigningKey` ≥ 32 characters, unique, rotation procedure documented
- [ ] `internalApiKey` strong; known only to in-cluster services
- [ ] RabbitMQ / Postgres / Redis passwords are production-grade (no `change-me` / `REPLACE_ME`)
- [ ] Mongo connection set if Logging (or other Mongo apps) enabled
- [ ] All `secrets.connectionStrings.*` point at correct host, database, and user
- [ ] Secrets are **not** in git — Vault / External Secrets / SealedSecrets
- [ ] Development admin (`admin@dev.local` / `DevAdmin!Pass1`) disabled or unreachable
- [ ] Production admin provisioned explicitly (not DevelopmentAdminSeeder)
- [ ] Demo tenant GUIDs are not used as production tenants

---

## 2. Data & migrate

- [ ] Postgres is managed or HA; disk encryption on; automated backups enabled
- [ ] Per-database **restore drill** completed (at least `identity` + `coordinator`)
- [ ] Schemas applied with `msf-migrate` and/or `migrate.enabled=true`
- [ ] Apps run with `ApplyMigrationsOnStartup=false` in Production
- [ ] Redis persistence / eviction policy is intentional (cache loss acceptable?)
- [ ] RabbitMQ: durable topology expectations + disk/memory alerts; credential rotation known

---

## 3. Cluster deploy

- [ ] `image.repositoryOwner` set; images use **immutable tags** (git SHA preferred; avoid `latest`)
- [ ] GHCR `imagePullSecrets` configured when packages are private
- [ ] Dedicated namespace with ResourceQuota / LimitRange
- [ ] Ingress enabled with **TLS** (cert-manager or existing certificate); HTTP redirects to HTTPS
- [ ] Gateway Service is `ClusterIP` behind Ingress; path routing covers Admin `/` and API prefixes
- [ ] Gateway replicas ≥ 2 (chart includes a PDB; a single pod is still a SPOF)
- [ ] Helm `NOTES.txt` shows **no** placeholder-secret warnings after install

---

## 4. Security surface

- [ ] Only Ingress is public; Postgres / Redis / RabbitMQ are not exposed on the public internet
- [ ] JWT issuer / audience / CORS match the production hostnames
- [ ] Rate limiting enabled for Production (`ServiceDefaults`)
- [ ] Swagger UI disabled or IP-restricted in Production
- [ ] [OWASP baseline](../deploy/security/owasp-baseline.md) reviewed for this release
- [ ] Logs do not contain passwords or raw tokens; Seq (or equivalent) access is RBAC-controlled

---

## 5. Observability & operations

- [ ] Structured logs sink (Seq or equivalent) with retention policy
- [ ] Distributed tracing (Jaeger/OTLP or alternative) reachable to operators
- [ ] Metrics scrape (Prometheus) + dashboards (Grafana or cloud APM)
- [ ] Alerts: CrashLoopBackOff, elevated 5xx, outbox DLQ &gt; 0, disk / DB connection saturation
- [ ] Runbooks written: migrate failure, DLQ requeue, JWT rotate, database restore
- [ ] On-call can reach Admin, Grafana, and log UI

---

## 6. Post-deploy smoke (≈15 minutes)

- [ ] `GET /ops/api/v1/health/services` — core services healthy / reachable
- [ ] Admin login with production admin + correct tenant
- [ ] Critical happy path works (e.g. RegisterUser saga or your first product flow)
- [ ] Outbox pending drains; DLQ empty or explained
- [ ] TLS + same-origin Admin → API (Ingress `/identity`, `/ops`, …)
- [ ] Missing / invalid JWT → 401; missing permission → 403

---

## Quick install reminders

```bash
# Prefer immutable tag from CI
helm upgrade --install msf ./deploy/helm/microservice-system \
  --namespace msf --create-namespace \
  -f values-production.local.yaml \
  --set image.repositoryOwner=<owner> \
  --set image.tag=<git-sha>
```

Never commit filled `values-production.local.yaml`. Start from `values-production.example.yaml`.

Schemas before relying on apps:

```bash
docker run --rm \
  -e POSTGRES_HOST=... -e POSTGRES_PASSWORD=... \
  ghcr.io/<owner>/msf-migrate:<tag>
```

Or `--set migrate.enabled=true` with `secrets.migratePostgresPassword` set.
