# microservice-system Helm chart

Kubernetes **application** deploy for images published by CI:

`ghcr.io/<owner>/msf-<name>:<tag>`

This chart ships Gateway, Admin SPA, and APIs. It is **not** a cluster operator UI and does **not** install Postgres/Redis/RabbitMQ/Mongo.

## Prerequisites

- External Postgres (databases from [`deploy/docker/postgres/init`](../../docker/postgres/init)), Redis, RabbitMQ
- MongoDB only if `apps.logging.enabled=true`
- Schemas applied via [`deploy/migrate`](../../migrate) / `msf-migrate` image (or `migrate.enabled=true`) **before** relying on Production apps (`ApplyMigrationsOnStartup=false`)
- GHCR pull access (`imagePullSecrets` if packages are private)
- Optional: Ingress controller when `ingress.enabled=true`

## Refuse placeholders (checklist)

Do **not** install to a real cluster until you have replaced:

- [ ] `image.repositoryOwner` (not `REPLACE_ME`)
- [ ] `secrets.jwtSigningKey` (≥ 32 chars, not `replace-with-…`)
- [ ] `secrets.internalApiKey`
- [ ] `secrets.rabbitmqPassword` (not `change-me`)
- [ ] All `secrets.connectionStrings.*` host/password
- [ ] `infrastructure.redisConnection` / `rabbitmq.host` (and mongo if logging)

Use [`values-production.example.yaml`](values-production.example.yaml) as a starting point.

## Install (lite)

Lite default: **gateway + admin + identity + user + coordinator**.

```bash
helm upgrade --install msf ./deploy/helm/microservice-system \
  --namespace msf --create-namespace \
  --set image.repositoryOwner=drnserhat \
  --set image.tag=latest \
  --set secrets.jwtSigningKey='<32+ chars>' \
  --set secrets.internalApiKey='<long key>' \
  --set secrets.rabbitmqPassword='<password>' \
  --set-string secrets.connectionStrings.identity='Host=postgres;Port=5432;Database=identity;Username=msf;Password=...'
```

Gateway Service defaults to `LoadBalancer` on port 80 and proxies to in-cluster `*-identity:8080`, etc.

Admin is ClusterIP. Without Ingress:

```bash
kubectl -n msf port-forward svc/msf-microservice-system-admin 5173:80
# open http://localhost:5173
```

(Release name / fullname may vary; use `kubectl get svc -n msf`.)

## Ingress (recommended for prod)

```bash
helm upgrade --install msf ./deploy/helm/microservice-system -n msf \
  -f values-production.example.yaml \
  --set image.repositoryOwner=<owner> \
  --set image.tag=<tag> \
  # …real secrets…
```

With Ingress enabled:

- `/` → Admin SPA
- `/identity`, `/ops`, `/settings`, … → Gateway (same-origin for the SPA)
- Set `apps.gateway.service.type=ClusterIP`

## Full stack

```bash
helm upgrade --install msf ./deploy/helm/microservice-system \
  --namespace msf --create-namespace \
  --set image.repositoryOwner=drnserhat \
  --set apps.notification.enabled=true \
  --set apps.file.enabled=true \
  --set apps.audit.enabled=true \
  --set apps.settings.enabled=true \
  --set apps.location.enabled=true \
  --set apps.logging.enabled=true
```

## Migrate Job

`migrate.enabled` defaults to `false`. Prefer CI or a one-shot:

```bash
docker run --rm \
  -e POSTGRES_HOST=… -e POSTGRES_PORT=5432 \
  -e POSTGRES_USER=msf -e POSTGRES_PASSWORD=… \
  ghcr.io/<owner>/msf-migrate:<tag>
```

When `migrate.enabled=true`, a Helm **pre-install/pre-upgrade** Job runs `msf-migrate` (requires `secrets.migratePostgresPassword`). Apps keep `ApplyMigrationsOnStartup=false`.

## Secrets

- Chart creates Secret `{{ release }}-*-secrets` from `values.secrets` / `infrastructure`.
- For vault wiring, see [`examples/external-secret.yaml`](examples/external-secret.yaml) (External Secrets Operator). Target name must match the chart Secret name (`fullname`-secrets).
- Do not commit production values files.

## Gateway PDB

`podDisruptionBudget.gateway.enabled` defaults to `true` with `minAvailable: 1`. With a single replica this blocks voluntary eviction until you scale gateway replicas.

## Verification checklist

1. `helm lint deploy/helm/microservice-system`
2. `helm template msf deploy/helm/microservice-system --set image.repositoryOwner=demo | grep -E 'kind: (Deployment|Ingress|Job)'`
3. After install: open Admin → login (`admin@dev.local` only when Identity Development seed is used — Production typically has no seed) → open Users or Settings
4. Confirm Gateway routes: `GET /ops/api/v1/health/services` with a valid JWT

## Images (CI)

| Image | Dockerfile |
|-------|------------|
| `msf-gateway` … `msf-logging` | service Dockerfiles |
| `msf-admin` | `apps/admin/Dockerfile` |
| `msf-migrate` | `deploy/migrate/Dockerfile` |
