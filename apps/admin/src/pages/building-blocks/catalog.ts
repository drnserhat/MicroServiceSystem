export type BuildingBlock = {
  id: string;
  name: string;
  purpose: string;
  dependencies: string[];
  usedBy: string[];
  version: string;
  docs?: string;
};

/** Inventory aligned with admin-ui-redesign-plan BuildingBlock Explorer */
export const BUILDING_BLOCKS: BuildingBlock[] = [
  {
    id: "authentication",
    name: "Authentication",
    purpose: "JWT + internal API key schemes for user and service-to-service calls.",
    dependencies: ["SharedKernel"],
    usedBy: ["Gateway", "All APIs"],
    version: "BuildingBlocks",
  },
  {
    id: "authorization",
    name: "Authorization",
    purpose: "Permission policies ([HasPermission]) over JWT claims.",
    dependencies: ["Authentication"],
    usedBy: ["All APIs", "Admin SPA"],
    version: "BuildingBlocks",
  },
  {
    id: "persistence",
    name: "Persistence",
    purpose: "EF Core base context, outbox/inbox stores, repositories, Unit of Work.",
    dependencies: ["SharedKernel"],
    usedBy: ["Identity", "User", "Settings", "Coordinator", "…"],
    version: "BuildingBlocks",
  },
  {
    id: "messaging",
    name: "Messaging",
    purpose: "RabbitMQ topology, publisher, consumers, outbox relay.",
    dependencies: ["Persistence", "Contracts"],
    usedBy: ["Coordinator", "All event publishers"],
    version: "BuildingBlocks",
  },
  {
    id: "caching",
    name: "Caching",
    purpose: "Memory/Redis cache abstractions with tenant-aware keys.",
    dependencies: ["SharedKernel"],
    usedBy: ["Gateway", "Services"],
    version: "BuildingBlocks",
  },
  {
    id: "storage",
    name: "Storage",
    purpose: "Object/file storage abstractions for File service.",
    dependencies: ["SharedKernel"],
    usedBy: ["File"],
    version: "BuildingBlocks",
  },
  {
    id: "logging",
    name: "Logging",
    purpose: "Structured logging helpers and sinks wiring.",
    dependencies: ["OpenTelemetry"],
    usedBy: ["All hosts"],
    version: "BuildingBlocks",
  },
  {
    id: "localization",
    name: "Localization",
    purpose: "Culture and resource string helpers.",
    dependencies: ["SharedKernel"],
    usedBy: ["APIs exposing user-facing messages"],
    version: "BuildingBlocks",
  },
  {
    id: "opentelemetry",
    name: "OpenTelemetry",
    purpose: "Traces/metrics/logs export (OTLP) to collector / Jaeger / Seq / Prom.",
    dependencies: [],
    usedBy: ["All hosts"],
    version: "BuildingBlocks",
    docs: "/observability/otel",
  },
  {
    id: "healthchecks",
    name: "HealthChecks",
    purpose: "ASP.NET health endpoints aggregated by Gateway /ops.",
    dependencies: [],
    usedBy: ["All hosts", "Gateway", "Admin Map"],
    version: "BuildingBlocks",
    docs: "/map",
  },
  {
    id: "saga",
    name: "Saga",
    purpose: "Orchestration runner, checkpoints, lease recovery.",
    dependencies: ["Persistence", "SharedKernel"],
    usedBy: ["Coordinator"],
    version: "BuildingBlocks",
    docs: "/workflows/definitions",
  },
  {
    id: "idempotency",
    name: "Idempotency",
    purpose: "Inbox / request idempotency guards for consumers and commands.",
    dependencies: ["Persistence"],
    usedBy: ["Event consumers"],
    version: "BuildingBlocks",
  },
  {
    id: "multitenancy",
    name: "MultiTenancy",
    purpose: "Tenant resolution and scoped data access.",
    dependencies: ["Authentication"],
    usedBy: ["All tenant-aware APIs"],
    version: "BuildingBlocks",
  },
  {
    id: "resilience",
    name: "Resilience",
    purpose: "Retry/circuit patterns for outbound HTTP and messaging.",
    dependencies: [],
    usedBy: ["Coordinator", "Gateway", "Publishers"],
    version: "BuildingBlocks",
  },
  {
    id: "application",
    name: "Application",
    purpose: "MediatR pipeline behaviors, validators, mapping conventions.",
    dependencies: ["SharedKernel"],
    usedBy: ["All Application layers"],
    version: "BuildingBlocks",
  },
  {
    id: "servicedefaults",
    name: "ServiceDefaults",
    purpose: "Host bootstrapping shared by service entrypoints.",
    dependencies: ["OpenTelemetry", "HealthChecks", "Logging"],
    usedBy: ["All service hosts"],
    version: "BuildingBlocks",
  },
  {
    id: "sharedkernel",
    name: "SharedKernel",
    purpose: "Result, pagination, specifications, domain primitives, saga base types.",
    dependencies: [],
    usedBy: ["Every layer"],
    version: "Shared",
  },
  {
    id: "contracts",
    name: "Contracts",
    purpose: "Integration event contracts across bounded contexts.",
    dependencies: [],
    usedBy: ["Publishers/consumers"],
    version: "Shared",
    docs: "/architecture/contracts",
  },
];

export function findBlock(id: string): BuildingBlock | undefined {
  return BUILDING_BLOCKS.find((b) => b.id === id);
}
