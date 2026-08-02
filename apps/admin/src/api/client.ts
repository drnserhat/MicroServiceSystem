import type { ApiResponse, AuthSession } from "./types";

const SESSION_KEY = "msf.admin.session";
const ACCESS_TOKEN_REFRESH_SKEW_MS = 60_000;

function isSessionExpiringSoon(session: AuthSession | null): boolean {
  if (!session?.accessTokenExpiresAtUtc) {
    return true;
  }

  const expiresAt = Date.parse(session.accessTokenExpiresAtUtc);
  if (!Number.isFinite(expiresAt)) {
    return true;
  }

  return Date.now() >= expiresAt - ACCESS_TOKEN_REFRESH_SKEW_MS;
}

export function getApiBaseUrl(): string {
  const configured = import.meta.env.VITE_API_BASE_URL?.trim();
  return configured ?? "";
}

export function loadSession(): AuthSession | null {
  const raw = localStorage.getItem(SESSION_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    localStorage.removeItem(SESSION_KEY);
    return null;
  }
}

export function saveSession(session: AuthSession): void {
  localStorage.setItem(SESSION_KEY, JSON.stringify(session));
}

export function clearSession(): void {
  localStorage.removeItem(SESSION_KEY);
}

export class ApiClientError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly failures?: Record<string, string[]>;

  constructor(
    message: string,
    status: number,
    code?: string,
    failures?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiClientError";
    this.status = status;
    this.code = code;
    this.failures = failures;
  }
}

export type RequestOptions = {
  method?: string;
  body?: unknown;
  token?: string | null;
  tenantId?: string | null;
  auth?: boolean;
  ifMatch?: string | number | null;
  formData?: FormData;
  rawResponse?: boolean;
};

let refreshHandler: (() => Promise<string | null>) | null = null;

export function setRefreshHandler(handler: (() => Promise<string | null>) | null): void {
  refreshHandler = handler;
}

export function formatEtag(version: string | number): string {
  const raw = String(version).replaceAll('"', "");
  return `"${raw}"`;
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {
    Accept: "application/json",
  };

  if (options.formData === undefined && options.body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  if (options.ifMatch !== undefined && options.ifMatch !== null && options.ifMatch !== "") {
    headers["If-Match"] = formatEtag(options.ifMatch);
  }

  const session = loadSession();
  let token = options.token ?? (options.auth === false ? null : session?.accessToken);
  const tenantId = options.tenantId ?? session?.tenantId;

  // Refresh before the access token expires so the first request does not 401 in DevTools.
  if (
    options.auth !== false &&
    options.token === undefined &&
    refreshHandler &&
    session?.accessToken &&
    isSessionExpiringSoon(session)
  ) {
    token = (await refreshHandler()) ?? token;
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  if (tenantId) {
    headers["X-Tenant-Id"] = tenantId;
  }

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    method:
      options.method ??
      (options.body !== undefined || options.formData !== undefined ? "POST" : "GET"),
    headers,
    body:
      options.formData ??
      (options.body !== undefined ? JSON.stringify(options.body) : undefined),
  });

  if (response.status === 401 && options.auth !== false && refreshHandler) {
    const nextToken = await refreshHandler();
    if (nextToken) {
      return apiRequest<T>(path, { ...options, token: nextToken });
    }
  }

  const payload = (await response.json().catch(() => null)) as ApiResponse<T> | null;

  if (!response.ok || !payload?.succeeded) {
    throw new ApiClientError(
      payload?.error?.description ?? `Request failed (${response.status})`,
      response.status,
      payload?.error?.code,
      payload?.error?.failures,
    );
  }

  return payload.data as T;
}

/** True when the error likely means the downstream service is not in the running stack. */
export function isServiceUnavailable(error: unknown): boolean {
  return error instanceof ApiClientError && (error.status === 502 || error.status === 503 || error.status === 404);
}
