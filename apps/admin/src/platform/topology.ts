export type TopologyNodeKind =
  | "edge"
  | "service"
  | "broker"
  | "cache"
  | "database"
  | "observability"
  | "console";

export type TopologyNode = {
  id: string;
  label: string;
  kind: TopologyNodeKind;
  layer: number;
  summary: string;
  /** Matches ops health cluster id when available */
  healthService?: string;
  versionLabel?: string;
  adminPath?: string;
  openApiPath?: string;
  dependsOn?: string[];
  metricsHint?: string;
};

export type TopologyEdge = {
  id: string;
  from: string;
  to: string;
  label?: string;
};

/** Runtime topology for Platform Map (static catalog + live health overlay). */
export const TOPOLOGY_NODES: TopologyNode[] = [
  {
    id: "gateway",
    label: "Gateway",
    kind: "edge",
    layer: 0,
    summary: "YARP reverse proxy, JWT edge, Swagger aggregation, /ops health.",
    versionLabel: "YARP",
    adminPath: "/platform",
    metricsHint: "Preview — latency via Prometheus",
  },
  {
    id: "identity",
    label: "Identity",
    kind: "service",
    layer: 1,
    summary: "Auth, tenants, roles, JWT, transactional outbox.",
    healthService: "identity",
    versionLabel: "core",
    adminPath: "/tenants",
    openApiPath: "/docs/identity/swagger.json",
    dependsOn: ["gateway", "postgres", "redis", "rabbitmq"],
  },
  {
    id: "user",
    label: "User",
    kind: "service",
    layer: 1,
    summary: "User profiles with ETag concurrency.",
    healthService: "user",
    versionLabel: "core",
    adminPath: "/users",
    openApiPath: "/docs/user/swagger.json",
    dependsOn: ["gateway", "postgres", "rabbitmq"],
  },
  {
    id: "coordinator",
    label: "Coordinator",
    kind: "service",
    layer: 1,
    summary: "RegisterUser saga orchestration + lease recovery.",
    healthService: "coordinator",
    versionLabel: "core",
    adminPath: "/workflows",
    dependsOn: ["gateway", "identity", "user", "postgres", "rabbitmq"],
  },
  {
    id: "settings",
    label: "Settings",
    kind: "service",
    layer: 1,
    summary: "Tenant key/value configuration CRUD.",
    healthService: "settings",
    versionLabel: "core",
    adminPath: "/settings",
    openApiPath: "/docs/settings/swagger.json",
    dependsOn: ["gateway", "postgres", "redis"],
  },
  {
    id: "notification",
    label: "Notification",
    kind: "service",
    layer: 2,
    summary: "Outbound notifications (full profile).",
    healthService: "notification",
    versionLabel: "addon",
    adminPath: "/notifications",
    dependsOn: ["rabbitmq", "postgres"],
  },
  {
    id: "audit",
    label: "Audit",
    kind: "service",
    layer: 2,
    summary: "Immutable audit trail (full profile).",
    healthService: "audit",
    versionLabel: "addon",
    adminPath: "/audit",
    dependsOn: ["rabbitmq", "postgres"],
  },
  {
    id: "logging",
    label: "Logging",
    kind: "service",
    layer: 2,
    summary: "System log browser backed by Mongo (full profile).",
    healthService: "logging",
    versionLabel: "addon",
    adminPath: "/logs",
    dependsOn: ["mongo"],
  },
  {
    id: "file",
    label: "File",
    kind: "service",
    layer: 2,
    summary: "Asset upload API (full profile).",
    healthService: "file",
    versionLabel: "addon",
    adminPath: "/files",
    dependsOn: ["postgres"],
  },
  {
    id: "location",
    label: "Location",
    kind: "service",
    layer: 2,
    summary: "Countries reference data (full profile).",
    healthService: "location",
    versionLabel: "addon",
    adminPath: "/countries",
    dependsOn: ["postgres"],
  },
  {
    id: "rabbitmq",
    label: "RabbitMQ",
    kind: "broker",
    layer: 3,
    summary: "Integration events, outbox relay, consumers.",
    versionLabel: "infra",
    adminPath: "/messaging",
    metricsHint: "Preview — queue depth via management API",
  },
  {
    id: "redis",
    label: "Redis",
    kind: "cache",
    layer: 3,
    summary: "Cache, distributed concerns.",
    versionLabel: "infra",
    metricsHint: "Preview — memory / hit rate",
  },
  {
    id: "postgres",
    label: "PostgreSQL",
    kind: "database",
    layer: 3,
    summary: "Per-service databases (EF Core + outbox/inbox).",
    versionLabel: "infra",
  },
  {
    id: "mongo",
    label: "MongoDB",
    kind: "database",
    layer: 3,
    summary: "Logging document store (full profile).",
    versionLabel: "infra",
  },
  {
    id: "observability",
    label: "Observability",
    kind: "observability",
    layer: 4,
    summary: "Seq, Jaeger, Prometheus, Grafana deep links.",
    versionLabel: "obs",
    adminPath: "/observability",
  },
  {
    id: "admin",
    label: "Admin Console",
    kind: "console",
    layer: 0,
    summary: "This MSF Platform Console (operator UI).",
    versionLabel: "SPA",
    adminPath: "/",
    dependsOn: ["gateway"],
  },
];

export const TOPOLOGY_EDGES: TopologyEdge[] = [
  { id: "e-gw-id", from: "gateway", to: "identity", label: "HTTP" },
  { id: "e-gw-user", from: "gateway", to: "user", label: "HTTP" },
  { id: "e-gw-coord", from: "gateway", to: "coordinator", label: "HTTP" },
  { id: "e-gw-set", from: "gateway", to: "settings", label: "HTTP" },
  { id: "e-coord-id", from: "coordinator", to: "identity", label: "saga" },
  { id: "e-coord-user", from: "coordinator", to: "user", label: "saga" },
  { id: "e-id-rabbit", from: "identity", to: "rabbitmq", label: "outbox" },
  { id: "e-user-rabbit", from: "user", to: "rabbitmq", label: "outbox" },
  { id: "e-rabbit-notif", from: "rabbitmq", to: "notification", label: "event" },
  { id: "e-rabbit-audit", from: "rabbitmq", to: "audit", label: "event" },
  { id: "e-id-pg", from: "identity", to: "postgres" },
  { id: "e-user-pg", from: "user", to: "postgres" },
  { id: "e-set-pg", from: "settings", to: "postgres" },
  { id: "e-id-redis", from: "identity", to: "redis" },
  { id: "e-log-mongo", from: "logging", to: "mongo" },
  { id: "e-svc-obs", from: "gateway", to: "observability", label: "OTLP" },
  { id: "e-admin-gw", from: "admin", to: "gateway", label: "JWT" },
];

export function getTopologyNode(id: string): TopologyNode | undefined {
  return TOPOLOGY_NODES.find((n) => n.id === id);
}

export function neighborsOf(id: string): { upstream: string[]; downstream: string[] } {
  const upstream = TOPOLOGY_EDGES.filter((e) => e.to === id).map((e) => e.from);
  const downstream = TOPOLOGY_EDGES.filter((e) => e.from === id).map((e) => e.to);
  return { upstream, downstream };
}
