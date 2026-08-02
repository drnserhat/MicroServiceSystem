import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  ACCESS_TOKEN_REFRESH_SKEW_MS,
  bootstrapAuthRefresh,
  getDefaultTenantId,
  isAccessTokenExpiringSoon,
  login as loginRequest,
  logout as logoutRequest,
  refreshSession,
  subscribeSession,
} from "@/api/auth";
import { loadSession } from "@/api/client";
import type { AuthSession } from "@/api/types";
import { hasAnyPermission, hasPermission, permissionsFromToken, rolesFromToken } from "./permissions";

bootstrapAuthRefresh();

type AuthContextValue = {
  session: AuthSession | null;
  isAuthenticated: boolean;
  permissions: string[];
  roles: string[];
  login: (email: string, password: string, tenantId: string) => Promise<void>;
  logout: () => void;
  defaultTenantId: string;
  can: (permission: string | string[]) => boolean;
  canAny: (permissions: string[]) => boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() => loadSession());

  useEffect(() => subscribeSession(setSession), []);

  useEffect(() => {
    if (!session?.accessTokenExpiresAtUtc) {
      return;
    }

    const expiresAt = Date.parse(session.accessTokenExpiresAtUtc);
    if (!Number.isFinite(expiresAt)) {
      return;
    }

    const delay = Math.max(5_000, expiresAt - Date.now() - ACCESS_TOKEN_REFRESH_SKEW_MS);
    const timer = window.setTimeout(() => {
      if (isAccessTokenExpiringSoon(loadSession())) {
        void refreshSession();
      }
    }, delay);

    return () => window.clearTimeout(timer);
  }, [session?.accessTokenExpiresAtUtc, session?.accessToken]);

  const login = useCallback(async (email: string, password: string, tenantId: string) => {
    const next = await loginRequest(email, password, tenantId);
    setSession(next);
  }, []);

  const logout = useCallback(() => {
    logoutRequest();
    setSession(null);
  }, []);

  const permissions = useMemo(
    () => permissionsFromToken(session?.accessToken),
    [session?.accessToken],
  );

  const roles = useMemo(() => rolesFromToken(session?.accessToken), [session?.accessToken]);

  const can = useCallback(
    (permission: string | string[]) => hasPermission(permissions, permission),
    [permissions],
  );

  const canAny = useCallback(
    (needed: string[]) => hasAnyPermission(permissions, needed),
    [permissions],
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isAuthenticated: Boolean(session?.accessToken),
      permissions,
      roles,
      login,
      logout,
      defaultTenantId: getDefaultTenantId(),
      can,
      canAny,
    }),
    [session, permissions, roles, login, logout, can, canAny],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return ctx;
}
