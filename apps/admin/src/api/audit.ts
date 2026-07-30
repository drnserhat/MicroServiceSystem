import { apiRequest } from "./client";
import type { AuditEntry, PagedResult } from "./types";

export function listAuditEntries(pageNumber = 1, pageSize = 20): Promise<PagedResult<AuditEntry>> {
  const query = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });

  return apiRequest<PagedResult<AuditEntry>>(`/audit/api/v1/audit?${query.toString()}`);
}

export function createAuditEntry(body: {
  action: string;
  resourceType: string;
  resourceId: string;
  actorUserId?: string;
  details?: string;
}): Promise<AuditEntry> {
  return apiRequest<AuditEntry>("/audit/api/v1/audit", {
    method: "POST",
    body,
  });
}
