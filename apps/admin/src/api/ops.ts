import { apiRequest } from "./client";
import type { HealthAggregate, OutboxSnapshot } from "./types";

export function getHealthAggregate(): Promise<HealthAggregate> {
  return apiRequest<HealthAggregate>("/ops/api/v1/health/services");
}

export function getOutboxSnapshot(take = 50): Promise<OutboxSnapshot> {
  const query = new URLSearchParams({ take: String(take) });
  return apiRequest<OutboxSnapshot>(`/identity/api/v1/ops/outbox?${query.toString()}`);
}

export function requeueDeadLetter(messageId: string): Promise<null> {
  return apiRequest<null>(`/identity/api/v1/ops/outbox/${messageId}/requeue`, {
    method: "POST",
  });
}
