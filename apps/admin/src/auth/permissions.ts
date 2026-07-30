import { FrameworkPermissions } from "@/auth/permissionCodes";

export function decodeJwtPayload(accessToken: string): Record<string, unknown> | null {
  try {
    const parts = accessToken.split(".");
    if (parts.length < 2 || !parts[1]) {
      return null;
    }

    const normalized = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=");
    const json = atob(padded);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

function collectClaim(payload: Record<string, unknown>, claim: string): string[] {
  const value = payload[claim];
  if (typeof value === "string") {
    return [value];
  }

  if (Array.isArray(value)) {
    return value.filter((item): item is string => typeof item === "string");
  }

  return [];
}

export function permissionsFromToken(accessToken: string | undefined | null): string[] {
  if (!accessToken) {
    return [];
  }

  const payload = decodeJwtPayload(accessToken);
  if (!payload) {
    return [];
  }

  return collectClaim(payload, "permission");
}

export function rolesFromToken(accessToken: string | undefined | null): string[] {
  if (!accessToken) {
    return [];
  }

  const payload = decodeJwtPayload(accessToken);
  if (!payload) {
    return [];
  }

  return collectClaim(payload, "role");
}

export function hasPermission(permissions: string[], required: string | string[]): boolean {
  const needed = Array.isArray(required) ? required : [required];
  return needed.every((code) => permissions.includes(code));
}

export function hasAnyPermission(permissions: string[], required: string[]): boolean {
  return required.some((code) => permissions.includes(code));
}

export { FrameworkPermissions };
