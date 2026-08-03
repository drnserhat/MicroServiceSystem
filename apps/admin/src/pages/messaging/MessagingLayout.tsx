import { useMemo } from "react";
import { Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { HubTabs, PageFrame } from "@/components/control";
import { ExternalToolLink } from "@/platform/tools";

export function MessagingLayout() {
  const { t } = useTranslation("messaging");
  const tabs = useMemo(
    () => [
      { to: "/messaging", label: t("overview"), end: true as const },
      { to: "/messaging/queues", label: t("queuesTitle") },
      { to: "/messaging/exchanges", label: t("exchangesTitle") },
      { to: "/messaging/bindings", label: t("bindingsTitle") },
      { to: "/messaging/publishers", label: t("publishersTitle") },
      { to: "/messaging/consumers", label: t("consumersTitle") },
      { to: "/messaging/dead-letters", label: t("deadLettersTitle") },
      { to: "/messaging/outbox", label: t("outboxTitle") },
      { to: "/messaging/inbox", label: t("inboxTitle") },
      { to: "/messaging/event-flow", label: t("eventFlowTitle") },
      { to: "/messaging/retries", label: t("retriesTitle") },
      { to: "/messaging/replay", label: t("replayTitle") },
      { to: "/messaging/inspect", label: t("inspectTitle") },
      { to: "/messaging/timeline", label: t("timelineTitle") },
    ],
    [t],
  );

  return (
    <RequirePermission permission={FrameworkPermissions.OpsOutboxRead}>
      <PageFrame
        pretitle={t("hubPretitle")}
        title={t("hubTitle")}
        description={t("hubDescription")}
        actions={
          <div className="btn-list">
            <ExternalToolLink id="rabbitmq" className="btn" />
          </div>
        }
      >
        <HubTabs tabs={tabs} label={t("hubTitle")} />
        <Outlet />
      </PageFrame>
    </RequirePermission>
  );
}
