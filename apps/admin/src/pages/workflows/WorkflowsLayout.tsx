import { useMemo } from "react";
import { Link, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { HubTabs, PageFrame } from "@/components/control";

export function WorkflowsLayout() {
  const { t } = useTranslation("workflows");
  const tabs = useMemo(
    () => [
      { to: "/workflows", label: t("overview"), end: true as const },
      { to: "/workflows/boards", label: t("boardsTitle") },
      { to: "/workflows/definitions", label: t("definitionsTitle") },
      { to: "/workflows/running", label: t("runningTitle") },
      { to: "/workflows/completed", label: t("completedTitle") },
      { to: "/workflows/failed", label: t("failedTitle") },
      { to: "/workflows/compensated", label: t("compensatedTitle") },
      { to: "/workflows/waiting", label: t("waitingTitle") },
      { to: "/workflows/retrying", label: t("retryingTitle") },
    ],
    [t],
  );

  return (
    <RequirePermission permission={FrameworkPermissions.OpsSagaRead}>
      <PageFrame
        pretitle={t("hubPretitle")}
        title={t("hubTitle")}
        description={t("hubDescription")}
        actions={
          <div className="btn-list">
            <Link className="btn" to="/messaging/outbox?service=coordinator">
              {t("outbox")}
            </Link>
            <Link className="btn btn-primary" to="/users/register">
              {t("startRegistration")}
            </Link>
          </div>
        }
      >
        <HubTabs tabs={tabs} label={t("hubTitle")} />
        <Outlet />
      </PageFrame>
    </RequirePermission>
  );
}
