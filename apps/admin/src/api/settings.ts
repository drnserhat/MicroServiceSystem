import { apiRequest } from "./client";
import type { PagedResult, SettingItem } from "./types";

export function listSettings(pageNumber = 1, pageSize = 20): Promise<PagedResult<SettingItem>> {
  const query = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });

  return apiRequest<PagedResult<SettingItem>>(`/settings/api/v1/settings?${query.toString()}`);
}

export function getSetting(key: string): Promise<SettingItem> {
  return apiRequest<SettingItem>(`/settings/api/v1/settings/${encodeURIComponent(key)}`);
}

export function upsertSetting(
  key: string,
  value: string,
  ifMatch?: number | null,
): Promise<SettingItem> {
  return apiRequest<SettingItem>("/settings/api/v1/settings", {
    method: "PUT",
    body: { key, value },
    ifMatch: ifMatch ?? undefined,
  });
}

export function deleteSetting(key: string, ifMatch: number): Promise<null> {
  return apiRequest<null>(`/settings/api/v1/settings/${encodeURIComponent(key)}`, {
    method: "DELETE",
    ifMatch,
  });
}
