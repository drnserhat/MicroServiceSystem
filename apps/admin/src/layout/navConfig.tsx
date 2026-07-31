import type { ReactNode } from "react";
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
  label: string;
  icon: ReactNode;
  permission?: string | string[];
  end?: boolean;
  keywords?: string[];
};

export type NavSection = {
  id: string;
  label: string;
  items: NavEntry[];
};

const icon = (node: ReactNode) => node;

/** Platform-first IA — scales to dozens of future services via Service Center, not flat nav. */
export const NAV_SECTIONS: NavSection[] = [
  {
    id: "platform",
    label: "Platform",
    items: [
      {
        to: "/",
        label: "Overview",
        icon: icon(<IconHome size={18} stroke={1.5} />),
        end: true,
        keywords: ["dashboard", "home", "status", "health"],
      },
      {
        to: "/map",
        label: "Platform Map",
        icon: icon(<IconRoute size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsHealthRead,
        keywords: ["topology", "graph", "runtime", "gateway"],
      },
      {
        to: "/platform",
        label: "Packages",
        icon: icon(<IconPackages size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsHealthRead,
        keywords: ["compose", "lite", "full", "obs"],
      },
      {
        to: "/services",
        label: "Services",
        icon: icon(<IconCube size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsHealthRead,
        keywords: ["microservice", "swagger", "openapi"],
      },
    ],
  },
  {
    id: "operations",
    label: "Operations",
    items: [
      {
        to: "/messaging",
        label: "Messaging",
        icon: icon(<IconStack2 size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsOutboxRead,
        keywords: ["outbox", "rabbitmq", "dlq", "inbox"],
      },
      {
        to: "/workflows",
        label: "Workflows",
        icon: icon(<IconGitBranch size={18} stroke={1.5} />),
        keywords: ["coordinator", "saga", "registeruser", "compensation"],
      },
      {
        to: "/users",
        label: "Users",
        icon: icon(<IconUsers size={18} stroke={1.5} />),
        permission: FrameworkPermissions.IdentityUsersRead,
      },
      {
        to: "/users/register",
        label: "Register user",
        icon: icon(<IconUserPlus size={18} stroke={1.5} />),
        permission: FrameworkPermissions.RegistrationUsersCreate,
      },
      {
        to: "/tenants",
        label: "Tenants",
        icon: icon(<IconBuilding size={18} stroke={1.5} />),
        permission: FrameworkPermissions.IdentityTenantsRead,
      },
      {
        to: "/roles",
        label: "Roles",
        icon: icon(<IconShield size={18} stroke={1.5} />),
        permission: FrameworkPermissions.IdentityRolesRead,
      },
    ],
  },
  {
    id: "observability",
    label: "Observability",
    items: [
      {
        to: "/observability",
        label: "Observability Hub",
        icon: icon(<IconBell size={18} stroke={1.5} />),
        keywords: ["otel", "grafana", "jaeger", "seq", "prometheus"],
      },
      {
        to: "/audit",
        label: "Audit",
        icon: icon(<IconClipboardList size={18} stroke={1.5} />),
        permission: FrameworkPermissions.AuditEntriesRead,
      },
      {
        to: "/logs",
        label: "System logs",
        icon: icon(<IconFileText size={18} stroke={1.5} />),
        permission: FrameworkPermissions.LoggingLogsRead,
      },
    ],
  },
  {
    id: "architecture",
    label: "Architecture",
    items: [
      {
        to: "/architecture",
        label: "Architecture Explorer",
        icon: icon(<IconTopologyStar3 size={18} stroke={1.5} />),
        keywords: ["bounded context", "contracts", "design-time"],
      },
      {
        to: "/building-blocks",
        label: "BuildingBlocks",
        icon: icon(<IconCube size={18} stroke={1.5} />),
        keywords: ["shared", "messaging", "persistence", "saga"],
      },
    ],
  },
  {
    id: "developer",
    label: "Developer",
    items: [
      {
        to: "/developer",
        label: "Developer Center",
        icon: icon(<IconCode size={18} stroke={1.5} />),
        keywords: ["msf-service", "msf-crud", "template", "scaffold"],
      },
    ],
  },
  {
    id: "reference-config",
    label: "Reference & Config",
    items: [
      {
        to: "/countries",
        label: "Countries",
        icon: icon(<IconMap2 size={18} stroke={1.5} />),
        permission: FrameworkPermissions.LocationCountriesRead,
      },
      {
        to: "/files",
        label: "File upload",
        icon: icon(<IconFileUpload size={18} stroke={1.5} />),
        permission: FrameworkPermissions.FileAssetsUpload,
      },
      {
        to: "/notifications",
        label: "Notifications",
        icon: icon(<IconMail size={18} stroke={1.5} />),
        permission: FrameworkPermissions.NotificationMessagesCreate,
      },
      {
        to: "/settings",
        label: "Tenant settings",
        icon: icon(<IconSettings size={18} stroke={1.5} />),
        permission: FrameworkPermissions.SettingsValuesRead,
      },
    ],
  },
];

/** Alias labels for redirected / dynamic paths not in primary nav */
const EXTRA_LABELS: Record<string, string> = {
  "/health": "Live health",
  "/map": "Platform Map",
  "/messaging/queues": "Queues",
  "/messaging/exchanges": "Exchanges",
  "/messaging/bindings": "Bindings",
  "/messaging/publishers": "Publishers",
  "/messaging/consumers": "Consumers",
  "/messaging/dead-letters": "Dead letters",
  "/messaging/outbox": "Outbox",
  "/messaging/inbox": "Inbox",
  "/messaging/event-flow": "Event flow",
  "/messaging/retries": "Retries",
  "/messaging/replay": "Replay",
  "/messaging/inspect": "Inspect message",
  "/messaging/timeline": "Messaging timeline",
  "/workflows/boards": "Boards",
  "/workflows/definitions": "Definitions",
  "/workflows/running": "Running",
  "/workflows/completed": "Completed",
  "/workflows/failed": "Failed",
  "/workflows/compensated": "Compensated",
  "/workflows/waiting": "Waiting",
  "/workflows/retrying": "Retrying",
  "/observability/metrics": "Metrics",
  "/observability/tracing": "Tracing",
  "/observability/logs": "Logs",
  "/observability/audit": "Audit",
  "/observability/errors": "Errors",
  "/observability/performance": "Performance",
  "/observability/otel": "OpenTelemetry",
  "/observability/prometheus": "Prometheus",
  "/observability/correlation": "Correlation",
  "/architecture/contexts": "Bounded contexts",
  "/architecture/dependencies": "Dependencies",
  "/architecture/events": "Events",
  "/architecture/event-flow": "Event flow",
  "/architecture/databases": "Databases",
  "/architecture/contracts": "Contracts",
  "/developer/service": "Create service",
  "/developer/aggregate": "Create aggregate",
  "/developer/crud": "Create CRUD",
  "/developer/event": "Create event",
  "/developer/saga": "Create saga",
  "/developer/building-block": "Create building block",
  "/developer/templates": "Templates",
};

export function flattenNav(): NavEntry[] {
  return NAV_SECTIONS.flatMap((section) => section.items);
}

/** Resolve a human label for breadcrumbs, favorites, and recent lists. */
export function resolvePathLabel(pathname: string): string {
  const normalized = pathname.length > 1 && pathname.endsWith("/") ? pathname.slice(0, -1) : pathname || "/";
  if (normalized === "/" || normalized === "") return "Overview";

  const exact = flattenNav().find((item) => item.to === normalized);
  if (exact) return exact.label;
  if (EXTRA_LABELS[normalized]) return EXTRA_LABELS[normalized]!;

  // Longest prefix match (e.g. /services/identity → Services)
  const prefixed = flattenNav()
    .filter((item) => item.to !== "/" && normalized.startsWith(`${item.to}/`))
    .sort((a, b) => b.to.length - a.to.length)[0];
  if (prefixed) {
    const rest = normalized.slice(prefixed.to.length + 1);
    return rest ? `${prefixed.label} / ${rest}` : prefixed.label;
  }

  // GUID-ish segments → shorten
  const parts = normalized.split("/").filter(Boolean);
  return parts
    .map((part) =>
      /^[0-9a-f-]{36}$/i.test(part) ? `${part.slice(0, 8)}…` : part.replace(/-/g, " "),
    )
    .join(" / ");
}

export function resolveBreadcrumbs(pathname: string): { label: string; to: string }[] {
  if (!pathname || pathname === "/") {
    return [{ label: "Overview", to: "/" }];
  }
  const parts = pathname.split("/").filter(Boolean);
  const crumbs: { label: string; to: string }[] = [{ label: "Overview", to: "/" }];
  let path = "";
  for (const part of parts) {
    path += `/${part}`;
    const exact = flattenNav().find((item) => item.to === path);
    if (exact) {
      crumbs.push({ label: exact.label, to: path });
    } else if (EXTRA_LABELS[path]) {
      crumbs.push({ label: EXTRA_LABELS[path]!, to: path });
    } else {
      const label = /^[0-9a-f-]{36}$/i.test(part) ? `${part.slice(0, 8)}…` : part;
      crumbs.push({ label, to: path });
    }
  }
  return crumbs;
}

/** @deprecated — Activity icon retained for any leftover imports */
export const HealthNavIcon = icon(<IconActivity size={18} stroke={1.5} />);
