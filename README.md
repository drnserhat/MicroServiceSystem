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
deploy/                      Docker and observability composition
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

# lite + observability only
docker compose -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  -f deploy/docker/docker-compose.resources.yml \
  --profile obs up -d --build
```

Host Postgres is published on **5433** by default (`POSTGRES_HOST_PORT`) so it does not clash with a local/other Postgres on 5432.

Gateway: `http://localhost:8080`

| Prefix | Service |
|--------|---------|
| `/identity` | Identity |
| `/user` | User |
| `/coordinator` | Coordinator |
| `/registration` | Coordinator RegisterUser saga |
| `/notification` | Notification |
| `/file` | File |
| `/audit` | Audit |
| `/settings` | Settings |
| `/location` | Location |
| `/logging` | Logging |

### Register a user (saga)

```bash
curl -X POST http://localhost:8080/registration \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
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
