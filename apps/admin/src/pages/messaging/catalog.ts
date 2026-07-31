export type QueuePreview = {
  name: string;
  type: "queue" | "exchange" | "binding";
  consumers: number | string;
  ready?: number;
  unacked?: number;
  durable?: boolean;
  note?: string;
};

export type PublisherPreview = {
  id: string;
  service: string;
  exchange: string;
  event: string;
};

export type ConsumerPreview = {
  id: string;
  service: string;
  queue: string;
  lag: string;
};

export type EventFlowHop = {
  id: string;
  label: string;
  detail: string;
};

/** Static Rabbit-style inventory for UI preview surfaces */
export const PREVIEW_QUEUES: QueuePreview[] = [
  { name: "identity.events", type: "queue", consumers: 1, ready: 0, unacked: 0, durable: true },
  { name: "user.events", type: "queue", consumers: 1, ready: 2, unacked: 0, durable: true },
  { name: "notification.events", type: "queue", consumers: 0, ready: 0, unacked: 0, durable: true, note: "full profile" },
  { name: "audit.events", type: "queue", consumers: 0, ready: 0, unacked: 0, durable: true, note: "full profile" },
];

export const PREVIEW_EXCHANGES: QueuePreview[] = [
  { name: "msf.integration", type: "exchange", consumers: "—", note: "topic" },
  { name: "msf.dead-letter", type: "exchange", consumers: "—", note: "fanout" },
];

export const PREVIEW_BINDINGS: QueuePreview[] = [
  { name: "UserRegistered → identity.events", type: "binding", consumers: "—" },
  { name: "UserProfileCreated → notification.events", type: "binding", consumers: "—", note: "full" },
  { name: "UserDisabled → user.events", type: "binding", consumers: "—" },
];

export const PREVIEW_PUBLISHERS: PublisherPreview[] = [
  { id: "p1", service: "Identity", exchange: "msf.integration", event: "UserRegistered" },
  { id: "p2", service: "User", exchange: "msf.integration", event: "UserProfileCreated" },
  { id: "p3", service: "Coordinator", exchange: "msf.integration", event: "RegistrationCompleted" },
];

export const PREVIEW_CONSUMERS: ConsumerPreview[] = [
  { id: "c1", service: "Notification", queue: "notification.events", lag: "—" },
  { id: "c2", service: "Audit", queue: "audit.events", lag: "—" },
  { id: "c3", service: "User", queue: "user.events", lag: "0" },
];

export const PREVIEW_EVENT_FLOW: EventFlowHop[] = [
  { id: "h1", label: "Identity", detail: "Outbox write in same UoW" },
  { id: "h2", label: "RabbitMQ", detail: "Relay publish" },
  { id: "h3", label: "Consumers", detail: "Inbox idempotency" },
  { id: "h4", label: "Projections", detail: "Audit / Notification / User" },
];

export const PREVIEW_INSPECT_MESSAGE = {
  id: "msg-preview-001",
  eventName: "UserRegistered",
  routingKey: "identity.user-registered",
  correlationId: "11111111-1111-1111-1111-111111111111",
  payload: `{
  "userId": "…",
  "email": "user@example.com",
  "tenantId": "11111111-1111-1111-1111-111111111111"
}`,
};

export type InboxPreview = {
  id: string;
  service: string;
  eventName: string;
  status: "Processed" | "Pending" | "Duplicate";
  receivedAt: string;
};

export const PREVIEW_INBOX: InboxPreview[] = [
  {
    id: "inbox-1",
    service: "User",
    eventName: "UserRegistered",
    status: "Processed",
    receivedAt: "2026-07-30T10:12:04Z",
  },
  {
    id: "inbox-2",
    service: "Notification",
    eventName: "UserProfileCreated",
    status: "Pending",
    receivedAt: "2026-07-30T10:12:06Z",
  },
  {
    id: "inbox-3",
    service: "Audit",
    eventName: "UserRegistered",
    status: "Duplicate",
    receivedAt: "2026-07-30T10:12:05Z",
  },
];

export type RetryPreview = {
  id: string;
  eventName: string;
  attempt: number;
  nextAt: string;
  reason: string;
};

export const PREVIEW_RETRIES: RetryPreview[] = [
  {
    id: "retry-1",
    eventName: "UserProfileCreated",
    attempt: 2,
    nextAt: "—",
    reason: "Transient HTTP 503 from Notification (preview)",
  },
];

