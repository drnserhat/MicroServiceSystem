# Distributed workflows: orchestration, choreography, and why we avoid 2PC

This document is the decision record for how multi-service workflows are built in
MicroServiceSystem. It maps the patterns from the tutorial repos to what the framework
actually ships.

## TL;DR

| Pattern | Use in this framework? | Where |
|---------|------------------------|--------|
| **Orchestration saga** | Yes — default for multi-step business transactions | `Coordinator` + `BuildingBlocks.Saga` |
| **Choreography** | Yes — for eventual side effects and fan-out | Integration events + outbox/inbox |
| **2PC (prepare/commit/rollback)** | No | — |
| **Sync HTTP data copy** | No (anti-pattern) | Prefer events |

---

## Orchestration saga

A **central orchestrator** owns the workflow state. It calls participants in order and
runs compensations in reverse when a step fails.

```
Client → Coordinator
            │
            ├─1─► Identity.Register
            ├─2─► User.CreateProfile
            │        └─ fail? → Identity.Disable (compensation)
            └─3─► publish Welcome + Audit (outbox)
```

**Use when:**
- Steps have a clear order and a single business outcome (e.g. “user is fully registered”)
- You need an audit trail of the workflow itself (`RegisterUserSaga` row)
- Compensation is asymmetric (undo A if B fails)

**Framework pieces:**
- Domain base: `SharedKernel.Primitives.SagaAggregateRoot<TState>`
- Step runner: `BuildingBlocks.Saga.ISagaStep<TContext>` + `SagaRunner`
- Durability: `ISagaCheckpoint` flushes saga state after each remote side effect and renews the saga
  lease as it goes; Coordinator `RegisterUserSagaRecoveryService` claims and finishes only sagas whose
  lease lapsed (`Saga:LeaseSeconds`), so a slow saga is never mistaken for an abandoned one
- Reference implementation: `Coordinator` `RegisterUserSaga` (`RegisterIdentityStep` → `CreateUserProfileStep`)

**Do not** put orchestration logic inside a random domain service. Keep it in Coordinator
(or a dedicated saga host) so bounded contexts stay unaware of the whole workflow.

---

## Choreography

There is **no central conductor**. Each service reacts to events and may emit further events.

```
OrderCreated ──► Stock
                   ├─ StockReserved ──► Payment
                   │                      ├─ PaymentCompleted ──► Order
                   │                      └─ PaymentFailed    ──► Order (+ Stock release)
                   └─ StockNotReserved ──► Order
```

This is the pattern in
[Microservices.Tutorial.Saga.Choreography.Example](https://github.com/drnserhat/Microservices.Tutorial.Saga.Choreography.Example)
and [Microservice.Example](https://github.com/drnserhat/Microservice.Example).

**Use when:**
- Side effects are eventually consistent (welcome email, audit trail, search index)
- Participants should stay decoupled (Notification must not be called synchronously by Identity)
- The workflow is a loose reaction chain, not a single transactional outcome owned by one service

**Framework pieces:**
- Contracts in `Shared/Contracts`
- Publish via `IIntegrationEventPublisher` (writes **outbox** in the same DB transaction)
- Consume via RabbitMQ + **inbox** idempotency
- Examples today: `UserRegistered` / `UserProfileCreated` → Audit projections; `UserDisabled` →
  deactivate profile + Audit; welcome/audit request handlers.
  Profile creation for registration is **orchestration-only** (Coordinator → User HTTP);
  do not also create profiles from `UserRegistered` or the two paths race.

**Rules:**
- Publishers never call downstream HTTP “just to sync data”
- Consumers must be idempotent (inbox)
- Prefer thin handlers that send application commands

---

## Why we do not productize 2PC

[Microservice.Tutorial.2PC.Example](https://github.com/drnserhat/Microservice.Tutorial.2PC.Example)
shows classic prepare → commit → rollback over HTTP with a coordinator tracking node readiness.

That is useful for **learning**. It is a poor default for microservices:

- Participants hold locks / reserved state across network round-trips
- Timeouts and partial commits are hard to reason about at scale
- Coupling is stronger than saga compensation (every node must speak Ready/Commit/Rollback)
- It fights “each service owns its database”

**Framework stance:** use **orchestration sagas with compensations** for write workflows that must
converge, and **choreography** for notifications and projections. Do not add a 2PC engine to
BuildingBlocks.

If a rare domain truly needs atomic multi-resource commit inside **one** service, use a local
database transaction (`IUnitOfWork`) — not distributed 2PC.

---

## Data synchronization

[Data_Synchronization_Examples](https://github.com/drnserhat/Data_Synchronization_Examples)
compares:

| Style | Tutorial idea | Framework stance |
|-------|---------------|------------------|
| **API-based** | Service A updates itself then HTTP-calls Service B to copy the change | **Avoid** — tight coupling, failure modes, no replay |
| **Event-based** | Service A publishes `UpdatePersonName`; Service B projects locally | **Prefer** — aligns with outbox/inbox |

When the same fact must appear in two bounded contexts, publish an integration event from the
owner and let others project. Do not open the other service’s database and do not require
synchronous “update twin” HTTP calls for consistency.

---

## Order / Stock / Payment (reference flow)

The Order→Stock→Payment tutorials
([choreography](https://github.com/drnserhat/Microservices.Tutorial.Saga.Choreography.Example),
[example](https://github.com/drnserhat/Microservice.Example)) are the canonical **choreography**
teaching flow. They are **not** copied into this repo as production services.

If we add a demo later, it should:
- Use framework messaging (outbox/inbox), not MassTransit-only in-process publish after `SaveChanges`
- Keep each service’s own database
- Express compensation as events (`PaymentFailed` → release stock) or an orchestration saga if
  the business needs a single tracked process instance

---

## Oracle

[Oracle.AutonomousDB.Example](https://github.com/drnserhat/Oracle.AutonomousDB.Example) is a raw
Oracle client sample. The framework’s persistence building block targets **PostgreSQL** (and Mongo
for logging). An Oracle provider is optional and only worth adding when a real product requires it.
Never embed connection strings or passwords in source.

---

## Decision cheat sheet

```
Need a tracked multi-step write with compensation?
  → Orchestration saga (Coordinator + SagaAggregateRoot + SagaRunner)

Need fan-out side effects / projections?
  → Choreography (integration events + outbox/inbox)

Need “all nodes prepare then commit”?
  → Don't. Redesign as saga + compensations.

Need the same field in two services?
  → Event-based projection. Not sync HTTP. Not shared DB.
```

## Related code

- `src/Shared/SharedKernel/Primitives/SagaAggregateRoot.cs`
- `src/BuildingBlocks/BuildingBlocks.Saga/`
- `src/Coordinator/`
- `src/BuildingBlocks/BuildingBlocks.Messaging/`
- `src/BuildingBlocks/BuildingBlocks.Persistence/Outbox/`
- `src/BuildingBlocks/BuildingBlocks.Persistence/Inbox/`
