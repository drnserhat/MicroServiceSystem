export type ApiError = {
  code: string;
  description: string;
  failures?: Record<string, string[]>;
};

export type ApiResponse<T> = {
  succeeded: boolean;
  data: T | null;
  error: ApiError | null;
  traceId?: string | null;
};

export type LoginResponse = {
  userId: string;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
};

export type SettingItem = {
  id: string;
  key: string;
  value: string;
  version: number;
};

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export type AuthSession = {
  userId: string;
  email?: string;
  accessToken: string;
  refreshToken: string;
  tenantId: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
};

export type UserProfile = {
  id: string;
  firstName: string;
  lastName: string;
  displayName: string;
  isActive: boolean;
  version: number;
};

export type RegistrationResult = {
  sagaId: string;
  userId: string;
  email: string;
  displayName: string;
};

export type AuditEntry = {
  id: string;
  action: string;
  resourceType: string;
  resourceId: string;
  actorUserId?: string | null;
  details?: string | null;
};

export type SystemLog = {
  id: string;
  tenantId: string;
  level: string;
  message: string;
  source?: string | null;
  correlationId?: string | null;
  timestamp: string;
};

export type Country = {
  id: string;
  code: string;
  name: string;
  version: number;
};

export type FileAsset = {
  id: string;
  fileName: string;
  container: string;
  path: string;
  sizeInBytes: number;
  storageProvider: string;
};

export type TenantItem = {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
};

export type IdentityUserItem = {
  id: string;
  email: string;
  userName: string;
  isActive: boolean;
  roleIds: string[];
};

export type RoleItem = {
  id: string;
  name: string;
  permissions: string[];
};

export type ServiceHealthItem = {
  service: string;
  status: string;
  description?: string | null;
  durationMs?: number | null;
  reachable: boolean;
};

export type HealthAggregate = {
  checkedAtUtc: string;
  services: ServiceHealthItem[];
};

export type OutboxSummary = {
  service: string;
  pendingCount: number;
  deadLetterCount: number;
};

export type OutboxDeadLetter = {
  id: string;
  eventName: string;
  occurredOnUtc: string;
  deadLetteredOnUtc?: string | null;
  attemptCount: number;
  error?: string | null;
  tenantId?: string | null;
  correlationId?: string | null;
};

export type OutboxSnapshot = {
  summary: OutboxSummary;
  deadLetters: OutboxDeadLetter[];
};
