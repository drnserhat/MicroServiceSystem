import type { ReactNode } from "react";
import {
  IconActivity,
  IconBuilding,
  IconCode,
  IconCube,
  IconFileText,
  IconFileUpload,
  IconGitBranch,
  IconHome,
  IconMail,
  IconMap2,
  IconPackages,
  IconSettings,
  IconShield,
  IconStack2,
  IconTopologyStar3,
  IconUserPlus,
  IconUsers,
  IconClipboardList,
  IconBell,
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

export const NAV_SECTIONS: NavSection[] = [
  {
    id: "overview",
    label: "Overview",
    items: [
      {
        to: "/",
        label: "Platform Overview",
        icon: icon(<IconHome size={18} stroke={1.5} />),
        end: true,
        keywords: ["dashboard", "home", "status"],
      },
    ],
  },
  {
    id: "platform",
    label: "Platform",
    items: [
      {
        to: "/platform",
        label: "Packages & health",
        icon: icon(<IconPackages size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsHealthRead,
        keywords: ["packages", "compose", "lite", "full"],
      },
      {
        to: "/health",
        label: "Live health",
        icon: icon(<IconActivity size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsHealthRead,
        keywords: ["ready", "probe"],
      },
    ],
  },
  {
    id: "services",
    label: "Services",
    items: [
      {
        to: "/services",
        label: "Service Center",
        icon: icon(<IconCube size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsHealthRead,
        keywords: ["microservice", "swagger"],
      },
    ],
  },
  {
    id: "messaging",
    label: "Messaging",
    items: [
      {
        to: "/messaging",
        label: "Messaging Center",
        icon: icon(<IconStack2 size={18} stroke={1.5} />),
        permission: FrameworkPermissions.OpsOutboxRead,
        keywords: ["outbox", "rabbitmq", "dlq"],
      },
    ],
  },
  {
    id: "workflows",
    label: "Workflows",
    items: [
      {
        to: "/workflows",
        label: "Saga Center",
        icon: icon(<IconGitBranch size={18} stroke={1.5} />),
        keywords: ["coordinator", "registeruser", "compensation"],
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
        keywords: ["otel", "grafana", "jaeger", "seq"],
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
    id: "identity",
    label: "Identity",
    items: [
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
    id: "configuration",
    label: "Configuration",
    items: [
      {
        to: "/settings",
        label: "Tenant settings",
        icon: icon(<IconSettings size={18} stroke={1.5} />),
        permission: FrameworkPermissions.SettingsValuesRead,
      },
    ],
  },
  {
    id: "reference",
    label: "Reference Data",
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
    ],
  },
  {
    id: "developer",
    label: "Developer Tools",
    items: [
      {
        to: "/developer",
        label: "Code generators",
        icon: icon(<IconCode size={18} stroke={1.5} />),
        keywords: ["msf-service", "msf-crud", "template"],
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
        keywords: ["graph", "topology"],
      },
      {
        to: "/building-blocks",
        label: "BuildingBlocks",
        icon: icon(<IconCube size={18} stroke={1.5} />),
        keywords: ["shared", "messaging", "persistence"],
      },
    ],
  },
];

export function flattenNav(): NavEntry[] {
  return NAV_SECTIONS.flatMap((section) => section.items);
}
