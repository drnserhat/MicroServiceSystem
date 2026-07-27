# MicroServiceSystem Framework

Enterprise ASP.NET Core microservice framework built on Clean Architecture, Onion Architecture, DDD,
CQRS and Event Driven Architecture. It is not an application: it is the reusable foundation that ERP,
CRM, HR, e-commerce, IoT, finance or logistics products are built on.

## Principles

The architectural constitution lives in [.cursor/agents/master-architect-agent.md](.cursor/agents/master-architect-agent.md).
Non negotiable rules:

- Every service owns its database. No shared database, no cross database query.
- Services communicate through APIs or integration events only.
- Domain never knows infrastructure; Application never references EF Core, MongoDB, Redis or RabbitMQ.
- Validation lives in FluentValidation pipeline behaviors, never inside handlers.
- Business rules live in the domain, never in controllers.
- Repositories are aggregate scoped. Generic repositories are off by default.
- State changes and published events are kept consistent by the outbox; consumers are idempotent through the inbox.
- Multi-service write workflows use **orchestration sagas** (Coordinator); fan-out side effects use **choreography** (integration events). Distributed 2PC is intentionally out of scope — see [docs/distributed-workflows.md](docs/distributed-workflows.md).

## Repository layout

```
src/
  Shared/SharedKernel        Domain primitives, Result pattern, specifications, pagination
  Shared/Contracts           Integration event contracts exchanged between services
  BuildingBlocks/            Cross cutting technical capabilities
  Gateway/                   YARP edge
  Coordinator/               Saga orchestration
  Services/                  Bounded contexts
templates/                   dotnet new templates for services and CRUD aggregates
tests/Architecture/          Architecture rules enforced on every build
deploy/                      Docker, Helm, migrate, secrets, observability
```

Each service follows the same shape:

```
Api            Composition root, controllers, middleware
Application    Use cases, CQRS handlers, ports, validators, mappings
Domain         Aggregates, value objects, domain events, specifications, business rules
Infrastructure Messaging, cache, external systems
Persistence    DbContext, configurations, migrations, repositories
tests          Unit and integration tests
```

## Technology

.NET 10, ASP.NET Core, EF Core, Dapper, PostgreSQL, MongoDB, Redis, RabbitMQ, MediatR, FluentValidation,
Mapster, Serilog, OpenTelemetry, Prometheus, Polly, YARP, JWT, Swagger, Docker, GitHub Actions.

MediatR is pinned to the last Apache 2.0 release so the framework stays redistributable without a
runtime license key.

## Getting started

```bash
dotnet build MicroServiceSystem.slnx
dotnet test MicroServiceSystem.slnx
```

### Local stack (Docker)

Default is a **lite** stack (postgres, redis, rabbitmq, identity, user, coordinator, gateway) with memory caps — suitable for Docker Desktop. Optional services and observability use Compose profiles.

```bash
# lite (~3–4 GB) — recommended for local work
docker compose -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  -f deploy/docker/docker-compose.resources.yml \
  up -d --build

# full stack (~8–12 GB): all services + mongo + Seq/Prometheus/Grafana
docker compose -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  -f deploy/docker/docker-compose.resources.yml \
  --profile full up -d --build

# lite + observability (Seq, Prometheus, Grafana, Jaeger)
docker compose -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  -f deploy/docker/docker-compose.resources.yml \
  -f deploy/docker/docker-compose.observability.yml \
  --profile obs up -d --build
```

With the observability overlay, apps export OTLP to Jaeger (`http://localhost:16686`). Prometheus scrapes `/metrics` on every service (`http://localhost:9090`); Grafana is at `http://localhost:3000` (admin/admin).

On push to `main`/`master`/`develop`, CI publishes container images to GHCR as `ghcr.io/<owner>/msf-<service>:<sha|branch|latest>`.

Cluster deploy example: [`deploy/helm/microservice-system`](deploy/helm/microservice-system) (lite: gateway + identity + user + coordinator; external Postgres/Redis/RabbitMQ). Apply migrations first, then:

```bash
helm upgrade --install msf ./deploy/helm/microservice-system \
  --namespace msf --create-namespace \
  --set image.repositoryOwner=<owner> \
  --set image.tag=<sha-or-latest>
```

Host Postgres is published on **5433** by default (`POSTGRES_HOST_PORT`) so it does not clash with a local/other Postgres on 5432.

### Production migrations & secrets

Production API `appsettings.json` files leave secrets empty and keep `ApplyMigrationsOnStartup=false`. Apply schemas before rollout, then inject secrets from the environment (or a vault):

```bash
# host / CI (defaults: localhost:5433, user/password msf)
./deploy/migrate/migrate-all.sh          # or migrate-all.ps1 on Windows

# one-shot migrate container against the compose network
docker compose -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.migrate.yml run --rm migrate

# apps with env-injected secrets (copy example.env → deploy/secrets/.env first)
docker compose --env-file deploy/secrets/.env \
  -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  -f deploy/docker/docker-compose.secrets.yml \
  up -d
```

Template variables live in `deploy/secrets/example.env` (real `.env` is gitignored). CI runs the migrate script against Postgres in the `migrate` job before the Docker compose validation job.

Gateway: `http://localhost:8080` (opens Swagger UI; choose a service from the dropdown — **Registration** is under **Coordinator**)

The gateway validates JWT by default. Anonymous through the edge: `POST /identity/api/v1/auth/login`, `POST /identity/api/v1/auth/refresh`. Health, `/docs/*`, and Dev Swagger stay anonymous; everything else needs `Authorization: Bearer`.

User profile reads return an `ETag` (and `version` in the JSON). Updates require the same value in `If-Match` or the gateway/API responds **428**; a stale version yields **409**.

Self-signup is closed: `POST /registration` requires a JWT with `registration.users.create` (tenant admin). Development seeds `admin@dev.local` / `DevAdmin!Pass1` for the demo tenant.

| Prefix | Service |
|--------|---------|
| `/identity` | Identity |
| `/user` | User |
| `/coordinator` | Coordinator |
| `/registration` | Coordinator RegisterUser saga |
| `/notification` | Notification |
| `/file` | File |
| `/audit` | Audit |
| `/settings` | Settings (list / get / upsert / delete; ETag + If-Match on update/delete) |
| `/location` | Location (countries CRUD + ETag/If-Match) |
| `/logging` | Logging (ingest + filtered list + get-by-id) |

Settings is the exemplar tenant CRUD: `GET /settings` is paged, `GET /settings/{key}` returns `ETag`, create via `PUT` without `If-Match`, update/delete require `If-Match` (428 if missing, 409 on conflict).

### Register a user (saga)

```bash
# 1) Login as the Development admin (seeded in Development)
TOKEN=$(curl -s -X POST http://localhost:8080/identity/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@dev.local\",\"password\":\"DevAdmin!Pass1\",\"tenantId\":\"11111111-1111-1111-1111-111111111111\"}" \
  | jq -r '.data.accessToken')

# 2) Provision a member (tenantId must match the admin's tenant)
curl -X POST http://localhost:8080/registration \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{\"email\":\"demo@example.com\",\"userName\":\"demo\",\"password\":\"Str0ng!Pass\",\"firstName\":\"Demo\",\"lastName\":\"User\",\"tenantId\":\"11111111-1111-1111-1111-111111111111\"}"
```

Coordinator creates the identity account, then the user profile, publishes welcome/audit events through the outbox, and compensates Identity if profile creation fails. Compensating Identity `disable` and User profile create/deactivate require `Authentication:InternalService:ApiKey` (`X-Internal-Api-Key`); the compose stack sets a shared dev key for Coordinator, Identity, and User.

## Adding a new service

```bash
dotnet new install ./templates/msf-service
dotnet new msf-service -n Product -o src/Services/Product --db postgres --publishes-events true
```

The generated service already contains layering, health checks, telemetry, tenant isolation, outbox
wiring and a Dockerfile.

## Adding a CRUD aggregate

```bash
dotnet new install ./templates/msf-crud
cd src/Services/Product
dotnet new msf-crud -n Category --service Product --route categories --permission-prefix product -o .
```

This generates the aggregate, domain events, specifications, repository port and implementation,
commands, queries, validators, handlers, mappings, EF configuration and the versioned controller.

## Multi tenancy

Tenant isolation is shared database per service with a `TenantId` discriminator. The tenant is resolved
from the access token first and from the gateway forwarded header second, then applied by EF Core global
query filters and tenant scoped cache keys.
