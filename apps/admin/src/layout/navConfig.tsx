import type { ReactNode } from "react";
import type { TFunction } from "i18next";
import {
  IconActivity,
  IconBell,
  IconBuilding,
  IconClipboardList,
  IconCode,
  IconCube,
  IconFileText,
  IconFileUpload,
  IconGitBranch,
  IconHome,
  IconMail,
  IconMap2,
  IconPackages,
  IconRoute,
  IconSettings,
  IconShield,
  IconStack2,
  IconTopologyStar3,
  IconUserPlus,
  IconUsers,
} from "@tabler/icons-react";
import { FrameworkPermissions } from "@/auth/permissionCodes";

export type NavEntry = {
  to: string;
  /** Key under the `nav` namespace */
  labelKey: string;
  icon: ReactNode;
  permission?: string | string[];
  end?: boolean;
  keywords?: string[];
};

export type NavSection = {
  id: string;
  /** Key under the `nav` namespace */
  labelKey: string;
  items: NavEntry[];
};

const icon = (node: ReactNode) => node;

/** Platform-first IA — scales to dozens of future services via Service Center, not flat nav. */
export const NAV_SECTIONS: NavSection[] = [
  {
    id: "platform",
    labelKey: "sectionPlatform",
    items: [
      {
        to: "/",
        labelKey: "overview",
        icon: icon(<IconHome size={18} stroke={1.5} />),
        end: true,
        keywords: ["dashboard", "home", "status", "health"],
      },
      {
        to: "/map",
        labelKey: "platformMap",
        icon: icon(<IconRoute size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsHealthRead,
        keywords: ["topology", "graph", "runtime", "gateway"],
      },
      {
        to: "/platform",
        labelKey: "packages",
        icon: icon(<IconPackages size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsHealthRead,
        keywords: ["compose", "lite", "full", "obs"],
      },
      {
        to: "/services",
        labelKey: "services",
        icon: icon(<IconCube size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsHealthRead,
        keywords: ["microservice", "swagger", "openapi"],
      },
    ],
  },
  {
    id: "operations",
    labelKey: "sectionOperations",
    items: [
      {
        to: "/messaging",
        labelKey: "messaging",
        icon: icon(<IconStack2 size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsOutboxRead,
        keywords: ["outbox", "rabbitmq", "dlq", "inbox"],
      },
      {
        to: "/workflows",
        labelKey: "workflows",
        icon: icon(<IconGitBranch size={18} stroke={1.5} />),
        keywords: ["coordinator", "saga", "registeruser", "compensation"],
      },
      {
        to: "/users",
        labelKey: "users",
        icon: icon(<IconUsers size={18} stroke={1.5} />),
        permission: FrameworkPermissions.IdentityUsersRead,
      },
      {
        to: "/users/register",
        labelKey: "registerUser",
        icon: icon(<IconUserPlus size={18} stroke={1.5} />),
        permission: FrameworkPermissions.RegistrationUsersCreate,
      },
      {
        to: "/tenants",
        labelKey: "tenants",
        icon: icon(<IconBuilding size={18} stroke={1.5} />),
        permission: FrameworkPermissions.IdentityTenantsRead,
      },
      {
        to: "/roles",
        labelKey: "roles",
        icon: icon(<IconShield size={18} stroke={1.5} />),
        permission: FrameworkPermissions.IdentityRolesRead,
      },
    ],
  },
  {
    id: "observability",
    labelKey: "sectionObservability",
    items: [
      {
        to: "/observability",
        labelKey: "observabilityHub",
        icon: icon(<IconBell size={18} stroke={1.5} />),
        keywords: ["otel", "grafana", "jaeger", "seq", "prometheus"],
      },
      {
        to: "/audit",
        labelKey: "audit",
        icon: icon(<IconClipboardList size={18} stroke={1.5} />),
        permission: FrameworkPermissions.AuditEntriesRead,
      },
      {
        to: "/logs",
        labelKey: "systemLogs",
        icon: icon(<IconFileText size={18} stroke={1.5} />),
        permission: FrameworkPermissions.LoggingLogsRead,
      },
    ],
  },
  {
    id: "architecture",
    labelKey: "sectionArchitecture",
    items: [
      {
        to: "/architecture",
        labelKey: "architectureExplorer",
        icon: icon(<IconTopologyStar3 size={18} stroke={1.5} />),
        keywords: ["bounded context", "contracts", "design-time"],
      },
      {
        to: "/building-blocks",
        labelKey: "buildingBlocks",
        icon: icon(<IconCube size={18} stroke={1.5} />),
        keywords: ["shared", "messaging", "persistence", "saga"],
      },
    ],
  },
  {
    id: "developer",
    labelKey: "sectionDeveloper",
    items: [
      {
        to: "/developer",
        labelKey: "developerCenter",
        icon: icon(<IconCode size={18} stroke={1.5} />),
        keywords: ["msf-service", "msf-crud", "template", "scaffold"],
      },
    ],
  },
  {
    id: "reference-config",
    labelKey: "sectionReferenceConfig",
    items: [
      {
        to: "/countries",
        labelKey: "countries",
        icon: icon(<IconMap2 size={18} stroke={1.5} />),
        permission: FrameworkPermissions.LocationCountriesRead,
      },
      {
        to: "/files",
        labelKey: "fileUpload",
        icon: icon(<IconFileUpload size={18} stroke={1.5} />),
        permission: FrameworkPermissions.FileAssetsUpload,
      },
      {
        to: "/notifications",
        labelKey: "notifications",
        icon: icon(<IconMail size={18} stroke={1.5} />),
        permission: FrameworkPermissions.NotificationMessagesCreate,
      },
      {
        to: "/settings",
        labelKey: "tenantSettings",
        icon: icon(<IconSettings size={18} stroke={1.5} />),
        permission: FrameworkPermissions.SettingsValuesRead,
      },
    ],
  },
];

/** Alias label keys for redirected / dynamic paths not in primary nav */
const EXTRA_LABEL_KEYS: Record<string, string> = {
  "/health": "liveHealth",
  "/map": "platformMap",
  "/messaging/queues": "queues",
  "/messaging/exchanges": "exchanges",
  "/messaging/bindings": "bindings",
  "/messaging/publishers": "publishers",
  "/messaging/consumers": "consumers",
  "/messaging/dead-letters": "deadLetters",
  "/messaging/outbox": "outbox",
  "/messaging/inbox": "inbox",
  "/messaging/event-flow": "eventFlow",
  "/messaging/retries": "retries",
  "/messaging/replay": "replay",
  "/messaging/inspect": "inspectMessage",
  "/messaging/timeline": "messagingTimeline",
  "/workflows/boards": "boards",
  "/workflows/definitions": "definitions",
  "/workflows/running": "running",
  "/workflows/completed": "completed",
  "/workflows/failed": "failed",
  "/workflows/compensated": "compensated",
  "/workflows/waiting": "waiting",
  "/workflows/retrying": "retrying",
  "/observability/metrics": "metrics",
  "/observability/tracing": "tracing",
  "/observability/logs": "logs",
  "/observability/audit": "audit",
  "/observability/errors": "errors",
  "/observability/performance": "performance",
  "/observability/otel": "openTelemetry",
  "/observability/prometheus": "prometheus",
  "/observability/correlation": "correlation",
  "/architecture/contexts": "boundedContexts",
  "/architecture/dependencies": "dependencies",
  "/architecture/events": "events",
  "/architecture/event-flow": "eventFlow",
  "/architecture/databases": "databases",
  "/architecture/contracts": "contracts",
  "/developer/service": "createService",
  "/developer/aggregate": "createAggregate",
  "/developer/crud": "createCrud",
  "/developer/event": "createEvent",
  "/developer/saga": "createSaga",
  "/developer/building-block": "createBuildingBlock",
  "/developer/templates": "templates",
};

export function flattenNav(): NavEntry[] {
  return NAV_SECTIONS.flatMap((section) => section.items);
}

function navLabel(t: TFunction, key: string): string {
  return t(`nav:${key}`);
}

/** Resolve a human label for breadcrumbs, favorites, and recent lists. */
export function resolvePathLabel(pathname: string, t: TFunction): string {
  const normalized = pathname.length > 1 && pathname.endsWith("/") ? pathname.slice(0, -1) : pathname || "/";
  if (normalized === "/" || normalized === "") return navLabel(t, "overview");

  const exact = flattenNav().find((item) => item.to === normalized);
  if (exact) return navLabel(t, exact.labelKey);
  if (EXTRA_LABEL_KEYS[normalized]) return navLabel(t, EXTRA_LABEL_KEYS[normalized]!);

  const prefixed = flattenNav()
    .filter((item) => item.to !== "/" && normalized.startsWith(`${item.to}/`))
    .sort((a, b) => b.to.length - a.to.length)[0];
  if (prefixed) {
    const rest = normalized.slice(prefixed.to.length + 1);
    const base = navLabel(t, prefixed.labelKey);
    return rest ? `${base} / ${rest}` : base;
  }

  const parts = normalized.split("/").filter(Boolean);
  return parts
    .map((part) =>
      /^[0-9a-f-]{36}$/i.test(part) ? `${part.slice(0, 8)}…` : part.replace(/-/g, " "),
    )
    .join(" / ");
}

export function resolveBreadcrumbs(pathname: string, t: TFunction): { label: string; to: string }[] {
  if (!pathname || pathname === "/") {
    return [{ label: navLabel(t, "overview"), to: "/" }];
  }
  const parts = pathname.split("/").filter(Boolean);
  const crumbs: { label: string; to: string }[] = [{ label: navLabel(t, "overview"), to: "/" }];
  let path = "";
  for (const part of parts) {
    path += `/${part}`;
    const exact = flattenNav().find((item) => item.to === path);
    if (exact) {
      crumbs.push({ label: navLabel(t, exact.labelKey), to: path });
    } else if (EXTRA_LABEL_KEYS[path]) {
      crumbs.push({ label: navLabel(t, EXTRA_LABEL_KEYS[path]!), to: path });
    } else {
      const label = /^[0-9a-f-]{36}$/i.test(part) ? `${part.slice(0, 8)}…` : part;
      crumbs.push({ label, to: path });
    }
  }
  return crumbs;
}

/** @deprecated — Activity icon retained for any leftover imports */
export const HealthNavIcon = icon(<IconActivity size={18} stroke={1.5} />);
