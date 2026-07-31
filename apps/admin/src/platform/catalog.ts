export type PlatformPackageKind = "core" | "addon" | "observability";

export type PlatformPackage = {
  id: string;
  name: string;
  kind: PlatformPackageKind;
  summary: string;
  gatewayPrefix?: string;
  adminPath?: string;
  composeNote: string;
  /** Matches gateway cluster id when health is available. */
  healthService?: string;
};

/** Catalog of MicroServiceSystem packages an operator can reason about from the admin UI. */
export const PLATFORM_PACKAGES: PlatformPackage[] = [
  {
    id: "gateway",
    name: "API Gateway",
    kind: "core",
    summary: "YARP reverse proxy, JWT edge, Swagger aggregation, health aggregate.",
    gatewayPrefix: "/",
    adminPath: "/map",
    composeNote: "Always on (lite).",
    healthService: undefined,
  },
  {
    id: "identity",
    name: "Identity",
    kind: "core",
    summary: "Auth, tenants, roles/permissions, JWT issue/refresh, outbox ops.",
    gatewayPrefix: "/identity",
    adminPath: "/tenants",
    composeNote: "Always on (lite).",
    healthService: "identity",
  },
  {
    id: "user",
    name: "User",
    kind: "core",
    summary: "User profiles (get/update with ETag).",
    gatewayPrefix: "/user",
    adminPath: "/users",
    composeNote: "Always on (lite).",
    healthService: "user",
  },
  {
    id: "coordinator",
    name: "Coordinator",
    kind: "core",
    summary: "RegisterUser saga orchestration.",
    gatewayPrefix: "/coordinator",
    adminPath: "/users/register",
    composeNote: "Always on (lite).",
    healthService: "coordinator",
  },
  {
    id: "settings",
    name: "Settings",
    kind: "core",
    summary: "Tenant key/value configuration CRUD (exemplar).",
    gatewayPrefix: "/settings",
    adminPath: "/settings",
    composeNote: "Always on (lite).",
    healthService: "settings",
  },
  {
    id: "admin",
    name: "Admin SPA",
    kind: "core",
    summary: "Tabler operator console (this UI).",
    composeNote: "Always on (lite) — http://localhost:5173",
  },
  {
    id: "audit",
    name: "Audit",
    kind: "addon",
    summary: "Immutable audit trail browser and write API.",
    gatewayPrefix: "/audit",
    adminPath: "/audit",
    composeNote: "Docker profile: full",
    healthService: "audit",
  },
  {
    id: "logging",
    name: "Logging",
    kind: "addon",
    summary: "Central system log store (Mongo) with filters.",
    gatewayPrefix: "/logging",
    adminPath: "/logs",
    composeNote: "Docker profile: full",
    healthService: "logging",
  },
  {
    id: "location",
    name: "Location",
    kind: "addon",
    summary: "Countries reference data CRUD + ETag.",
    gatewayPrefix: "/location",
    adminPath: "/countries",
    composeNote: "Docker profile: full",
    healthService: "location",
  },
  {
    id: "file",
    name: "File",
    kind: "addon",
    summary: "Asset upload to configured storage provider.",
    gatewayPrefix: "/file",
    adminPath: "/files",
    composeNote: "Docker profile: full",
    healthService: "file",
  },
  {
    id: "notification",
    name: "Notification",
    kind: "addon",
    summary: "Outbound notification create (welcome / messages).",
    gatewayPrefix: "/notification",
    adminPath: "/notifications",
    composeNote: "Docker profile: full",
    healthService: "notification",
  },
  {
    id: "mongodb",
    name: "MongoDB",
    kind: "addon",
    summary: "Backing store for Logging (and similar document workloads).",
    composeNote: "Docker profile: full",
  },
  {
    id: "seq",
    name: "Seq",
    kind: "observability",
    summary: "Structured log UI for developers/operators.",
    composeNote: "Docker profile: obs / full — http://localhost:5341",
  },
  {
    id: "jaeger",
    name: "Jaeger",
    kind: "observability",
    summary: "Distributed tracing UI.",
    composeNote: "Docker profile: obs / full — http://localhost:16686",
  },
  {
    id: "prometheus",
    name: "Prometheus",
    kind: "observability",
    summary: "Metrics scrape and query.",
    composeNote: "Docker profile: obs / full — http://localhost:9090",
  },
  {
    id: "grafana",
    name: "Grafana",
    kind: "observability",
    summary: "Dashboards over Prometheus/Seq.",
    composeNote: "Docker profile: obs / full — http://localhost:3000",
  },
  {
    id: "rabbitmq",
    name: "RabbitMQ",
    kind: "core",
    summary: "Broker for integration events / outbox relay.",
    adminPath: "/messaging",
    composeNote: "Always on (lite) — management http://localhost:15672",
  },
  {
    id: "postgres",
    name: "PostgreSQL",
    kind: "core",
    summary: "Primary relational store for EF Core services.",
    composeNote: "Always on (lite).",
  },
  {
    id: "redis",
    name: "Redis",
    kind: "core",
    summary: "Cache / distributed primitives.",
    composeNote: "Always on (lite).",
  },
];
