import { apiRequest } from "./client";
import type { IdentityUserItem, PagedResult, RoleItem, TenantItem } from "./types";

export function listTenants(pageNumber = 1, pageSize = 20, search = ""): Promise<PagedResult<TenantItem>> {
  const query = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  if (search) query.set("search", search);

  return apiRequest<PagedResult<TenantItem>>(`/identity/api/v1/tenants?${query.toString()}`);
}

export function createTenant(body: {
  name: string;
  slug: string;
  tenantId?: string;
}): Promise<TenantItem> {
  return apiRequest<TenantItem>("/identity/api/v1/tenants/admin", {
    method: "POST",
    body,
  });
}

export function setTenantActive(tenantId: string, isActive: boolean): Promise<TenantItem> {
  return apiRequest<TenantItem>(`/identity/api/v1/tenants/${tenantId}/activation`, {
    method: "POST",
    body: { isActive },
  });
}

export function listIdentityUsers(
  pageNumber = 1,
  pageSize = 20,
  search = "",
): Promise<PagedResult<IdentityUserItem>> {
  const query = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  if (search) query.set("search", search);

  return apiRequest<PagedResult<IdentityUserItem>>(`/identity/api/v1/users?${query.toString()}`);
}

export function disableIdentityUser(userId: string, reason: string): Promise<null> {
  return apiRequest<null>(`/identity/api/v1/users/${userId}/disable`, {
    method: "POST",
    body: { reason },
  });
}

export function listRoles(): Promise<RoleItem[]> {
  return apiRequest<RoleItem[]>("/identity/api/v1/roles");
}

export function assignUserRole(userId: string, roleId: string): Promise<null> {
  return apiRequest<null>(`/identity/api/v1/users/${userId}/roles/${roleId}`, {
    method: "POST",
  });
}

export function unassignUserRole(userId: string, roleId: string): Promise<null> {
  return apiRequest<null>(`/identity/api/v1/users/${userId}/roles/${roleId}`, {
    method: "DELETE",
  });
}
