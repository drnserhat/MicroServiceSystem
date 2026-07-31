import { apiRequest, clearSession, loadSession, saveSession, setRefreshHandler } from "./client";
import type { AuthSession, LoginResponse } from "./types";

const defaultTenantId =
  import.meta.env.VITE_DEFAULT_TENANT_ID?.trim() ||
  "11111111-1111-1111-1111-111111111111";

export function getDefaultTenantId(): string {
  return defaultTenantId;
}

export async function login(email: string, password: string, tenantId: string): Promise<AuthSession> {
  const data = await apiRequest<LoginResponse>("/identity/api/v1/auth/login", {
    method: "POST",
    auth: false,
    tenantId,
    body: { email, password, tenantId },
  });

  const session: AuthSession = {
    userId: data.userId,
    email,
    accessToken: data.accessToken,
    refreshToken: data.refreshToken,
    tenantId,
    accessTokenExpiresAtUtc: data.accessTokenExpiresAtUtc,
    refreshTokenExpiresAtUtc: data.refreshTokenExpiresAtUtc,
  };

  saveSession(session);
  return session;
}

/** In-flight refresh so parallel 401s share one rotation (Identity refresh is single-winner). */
let refreshInFlight: Promise<string | null> | null = null;

export async function refreshSession(): Promise<string | null> {
  if (refreshInFlight) {
    return refreshInFlight;
  }

  refreshInFlight = (async () => {
    const current = loadSession();
    if (!current?.refreshToken) {
      clearSession();
      return null;
    }

    try {
      const data = await apiRequest<LoginResponse>("/identity/api/v1/auth/refresh", {
        method: "POST",
        auth: false,
        tenantId: current.tenantId,
        body: {
          refreshToken: current.refreshToken,
          tenantId: current.tenantId,
        },
      });

      const next: AuthSession = {
        ...current,
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
        accessTokenExpiresAtUtc: data.accessTokenExpiresAtUtc,
        refreshTokenExpiresAtUtc: data.refreshTokenExpiresAtUtc,
      };

      saveSession(next);
      return next.accessToken;
    } catch {
      clearSession();
      return null;
    }
  })();

  try {
    return await refreshInFlight;
  } finally {
    refreshInFlight = null;
  }
}

export function logout(): void {
  clearSession();
}

export function bootstrapAuthRefresh(): void {
  setRefreshHandler(refreshSession);
}
