import { apiRequest, clearSession, loadSession, saveSession, setRefreshHandler } from "./client";
import type { AuthSession, LoginResponse } from "./types";

const defaultTenantId =
  import.meta.env.VITE_DEFAULT_TENANT_ID?.trim() ||
  "11111111-1111-1111-1111-111111111111";

/** Refresh this many ms before access-token expiry to avoid a console 401 + retry. */
export const ACCESS_TOKEN_REFRESH_SKEW_MS = 60_000;

type SessionListener = (session: AuthSession | null) => void;
const sessionListeners = new Set<SessionListener>();

export function subscribeSession(listener: SessionListener): () => void {
  sessionListeners.add(listener);
  return () => {
    sessionListeners.delete(listener);
  };
}

function publishSession(session: AuthSession | null): void {
  for (const listener of sessionListeners) {
    listener(session);
  }
}

function persistSession(session: AuthSession): void {
  saveSession(session);
  publishSession(session);
}

function dropSession(): void {
  clearSession();
  publishSession(null);
}

export function getDefaultTenantId(): string {
  return defaultTenantId;
}

export function isAccessTokenExpiringSoon(
  session: AuthSession | null | undefined,
  skewMs = ACCESS_TOKEN_REFRESH_SKEW_MS,
): boolean {
  if (!session?.accessTokenExpiresAtUtc) {
    return true;
  }

  const expiresAt = Date.parse(session.accessTokenExpiresAtUtc);
  if (!Number.isFinite(expiresAt)) {
    return true;
  }

  return Date.now() >= expiresAt - skewMs;
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

  persistSession(session);
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
      dropSession();
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

      persistSession(next);
      return next.accessToken;
    } catch {
      dropSession();
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
  dropSession();
}

export function bootstrapAuthRefresh(): void {
  setRefreshHandler(refreshSession);
}
