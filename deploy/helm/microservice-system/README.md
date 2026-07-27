# microservice-system Helm chart

Minimal Kubernetes deploy for images published by CI:

`ghcr.io/<owner>/msf-<name>:<tag>`

## Prerequisites

- External Postgres (databases from `deploy/docker/postgres/init`), Redis, RabbitMQ
- MongoDB only if `apps.logging.enabled=true`
- Schemas applied via `deploy/migrate` (or CI migrate job) before apps start
- GHCR pull access (`imagePullSecrets` if the packages are private)

## Install (lite)

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

Gateway listens on Service port 80 (`LoadBalancer` by default) and proxies to in-cluster `*-identity:8080`, `*-user:8080`, etc.

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

`migrate.enabled` defaults to `false`. Prefer CI/`deploy/migrate`. If enabled, the Job image must contain the repository checkout and `deploy/migrate/migrate-all.sh` (same idea as `docker-compose.migrate.yml`).

## Secrets

Replace placeholder values in `values.yaml` or inject via ExternalSecrets / Sealed Secrets. Do not commit production secrets.
