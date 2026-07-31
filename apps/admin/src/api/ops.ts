import { apiRequest } from "./client";
import type { HealthAggregate, InboxSummary, OutboxSnapshot, SagaItem, SagaList } from "./types";

/** Gateway path prefixes that expose Identity-shaped outbox/inbox ops (Phase 11). */
export const OUTBOX_SERVICES = [
  "identity",
  "user",
  "settings",
  "coordinator",
  "audit",
  "notification",
  "file",
  "location",
] as const;

export type OutboxService = (typeof OUTBOX_SERVICES)[number];

export function getHealthAggregate(): Promise<HealthAggregate> {
  return apiRequest<HealthAggregate>("/ops/api/v1/health/services");
}

export function getOutboxSnapshot(service: OutboxService = "identity", take = 50): Promise<OutboxSnapshot> {
  const query = new URLSearchParams({ take: String(take) });
  return apiRequest<OutboxSnapshot>(`/${service}/api/v1/ops/outbox?${query.toString()}`);
}

export function requeueDeadLetter(service: OutboxService, messageId: string): Promise<null> {
  return apiRequest<null>(`/${service}/api/v1/ops/outbox/${messageId}/requeue`, {
    method: "POST",
  });
}

export function getInboxSummary(service: OutboxService): Promise<InboxSummary> {
  return apiRequest<InboxSummary>(`/${service}/api/v1/ops/inbox/summary`);
}

export function listSagas(params?: { state?: string; take?: number }): Promise<SagaList> {
  const query = new URLSearchParams();
  if (params?.state) query.set("state", params.state);
  query.set("take", String(params?.take ?? 50));
  return apiRequest<SagaList>(`/coordinator/api/v1/ops/sagas?${query.toString()}`);
}

export function getSaga(id: string): Promise<SagaItem> {
  return apiRequest<SagaItem>(`/coordinator/api/v1/ops/sagas/${id}`);
}
