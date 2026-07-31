export type ArchNode = { id: string; label: string; meta: string; to?: string };

export const ARCH_CANVAS_ROWS: ArchNode[][] = [
  [{ id: "gateway", label: "Gateway", meta: "YARP + JWT + /ops", to: "/map" }],
  [
    { id: "identity", label: "Identity", meta: "Auth · tenants", to: "/tenants" },
    { id: "user", label: "User", meta: "Profiles", to: "/users" },
    { id: "coordinator", label: "Coordinator", meta: "Sagas", to: "/workflows" },
    { id: "settings", label: "Settings", meta: "KV CRUD", to: "/settings" },
  ],
  [
    { id: "audit", label: "Audit", meta: "full", to: "/audit" },
    { id: "logging", label: "Logging", meta: "full", to: "/logs" },
    { id: "notification", label: "Notification", meta: "full", to: "/notifications" },
    { id: "file", label: "File", meta: "full", to: "/files" },
    { id: "location", label: "Location", meta: "full", to: "/countries" },
  ],
  [
    { id: "rabbit", label: "RabbitMQ", meta: "Events", to: "/messaging" },
    { id: "redis", label: "Redis", meta: "Cache" },
    { id: "pg", label: "PostgreSQL", meta: "EF Core" },
    { id: "mongo", label: "MongoDB", meta: "Logging" },
  ],
];

export type BoundedContext = {
  id: string;
  name: string;
  owns: string;
  publishes: string[];
  consumes: string[];
  db: string;
  to?: string;
};

export const BOUNDED_CONTEXTS: BoundedContext[] = [
  {
    id: "identity",
    name: "Identity",
    owns: "Users (auth), tenants, roles, refresh tokens, outbox",
    publishes: ["UserRegistered", "UserDisabled"],
    consumes: [],
    db: "PostgreSQL (Identity)",
    to: "/services/identity",
  },
  {
    id: "user",
    name: "User",
    owns: "User profiles",
    publishes: ["UserProfileCreated", "UserProfileUpdated"],
    consumes: ["UserRegistered"],
    db: "PostgreSQL (User)",
    to: "/services/user",
  },
  {
    id: "coordinator",
    name: "Coordinator",
    owns: "RegisterUserSaga checkpoints / leases",
    publishes: ["RegistrationCompleted (conceptual)"],
    consumes: [],
    db: "PostgreSQL (Coordinator)",
    to: "/workflows",
  },
  {
    id: "settings",
    name: "Settings",
    owns: "Tenant key/value settings",
    publishes: [],
    consumes: [],
    db: "PostgreSQL (Settings)",
    to: "/settings",
  },
  {
    id: "audit",
    name: "Audit",
    owns: "Immutable audit entries",
    publishes: [],
    consumes: ["* domain events (full)"],
    db: "PostgreSQL (Audit)",
    to: "/audit",
  },
  {
    id: "logging",
    name: "Logging",
    owns: "System log projections",
    publishes: [],
    consumes: ["OTLP / ingest"],
    db: "MongoDB (Logging)",
    to: "/logs",
  },
  {
    id: "notification",
    name: "Notification",
    owns: "Notification outbox / delivery records",
    publishes: [],
    consumes: ["UserProfileCreated / welcome"],
    db: "PostgreSQL (Notification)",
    to: "/notifications",
  },
  {
    id: "file",
    name: "File",
    owns: "File metadata",
    publishes: [],
    consumes: [],
    db: "PostgreSQL + object store",
    to: "/files",
  },
  {
    id: "location",
    name: "Location",
    owns: "Countries / reference geo",
    publishes: [],
    consumes: [],
    db: "PostgreSQL (Location)",
    to: "/countries",
  },
];

export type ArchDependency = { from: string; to: string; via: string };

export const ARCH_DEPENDENCIES: ArchDependency[] = [
  { from: "Client / Admin", to: "Gateway", via: "HTTPS + JWT" },
  { from: "Gateway", to: "Identity / User / …", via: "YARP clusters" },
  { from: "Coordinator", to: "Identity", via: "HTTP (register / disable)" },
  { from: "Coordinator", to: "User", via: "HTTP (create profile)" },
  { from: "Services", to: "RabbitMQ", via: "Outbox relay" },
  { from: "Consumers", to: "Inbox store", via: "Idempotent handlers" },
  { from: "All hosts", to: "OTel pipeline", via: "OTLP → Seq/Jaeger/Prom" },
];

export type IntegrationEvent = {
  name: string;
  owner: string;
  contract: string;
  note?: string;
};

export const INTEGRATION_EVENTS: IntegrationEvent[] = [
  { name: "UserRegistered", owner: "Identity", contract: "Shared/Contracts", note: "Starts profile path" },
  { name: "UserProfileCreated", owner: "User", contract: "Shared/Contracts", note: "Welcome / audit fan-out" },
  { name: "UserProfileUpdated", owner: "User", contract: "Shared/Contracts" },
  { name: "UserDisabled", owner: "Identity", contract: "Shared/Contracts", note: "Compensation / admin" },
];

export type DbOwnership = {
  store: string;
  owners: string[];
  note: string;
};

export const DB_OWNERSHIP: DbOwnership[] = [
  { store: "PostgreSQL — Identity", owners: ["Identity"], note: "Auth + outbox/inbox" },
  { store: "PostgreSQL — User", owners: ["User"], note: "Profiles only" },
  { store: "PostgreSQL — Coordinator", owners: ["Coordinator"], note: "Saga state + leases" },
  { store: "PostgreSQL — Settings", owners: ["Settings"], note: "KV exemplar" },
  { store: "PostgreSQL — Audit / Notif / File / Location", owners: ["Addon services"], note: "full profile" },
  { store: "MongoDB — Logging", owners: ["Logging"], note: "full / obs profile" },
  { store: "Redis", owners: ["Gateway / Caching"], note: "Cache — not system of record" },
  { store: "RabbitMQ", owners: ["Messaging BB"], note: "Transport — durable queues" },
];
