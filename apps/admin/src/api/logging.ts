import { apiRequest } from "./client";
import type { PagedResult, SystemLog } from "./types";

export type LogFilters = {
  level?: string;
  source?: string;
  correlationId?: string;
  fromUtc?: string;
  toUtc?: string;
  pageNumber?: number;
  pageSize?: number;
};

export function listLogs(filters: LogFilters = {}): Promise<PagedResult<SystemLog>> {
  const query = new URLSearchParams({
    pageNumber: String(filters.pageNumber ?? 1),
    pageSize: String(filters.pageSize ?? 20),
  });

  if (filters.level) query.set("level", filters.level);
  if (filters.source) query.set("source", filters.source);
  if (filters.correlationId) query.set("correlationId", filters.correlationId);
  if (filters.fromUtc) query.set("fromUtc", filters.fromUtc);
  if (filters.toUtc) query.set("toUtc", filters.toUtc);

  return apiRequest<PagedResult<SystemLog>>(`/logging/api/v1/logs?${query.toString()}`);
}

export function getLog(id: string): Promise<SystemLog> {
  return apiRequest<SystemLog>(`/logging/api/v1/logs/${id}`);
}
