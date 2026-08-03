import { useMemo } from "react";
import { Link, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { HubTabs, PageFrame } from "@/components/control";

export function ArchitectureLayout() {
  const { t } = useTranslation(["architecture", "platform"]);
  const tabs = useMemo(
    () => [
      { to: "/architecture", label: t("overview"), end: true as const },
      { to: "/architecture/contexts", label: t("boundedContexts") },
      { to: "/architecture/dependencies", label: t("dependencies") },
      { to: "/architecture/events", label: t("events") },
      { to: "/architecture/event-flow", label: t("eventFlow") },
      { to: "/architecture/databases", label: t("databases") },
      { to: "/architecture/contracts", label: t("contracts") },
    ],
    [t],
  );

  return (
    <PageFrame
      pretitle={t("hubPretitle")}
      title={t("hubTitle")}
      description={t("hubDescription")}
      actions={
        <div className="btn-list">
          <Link className="btn" to="/map">
            {t("platform:platformMap")}
          </Link>
          <Link className="btn" to="/building-blocks">
            {t("buildingBlocks")}
          </Link>
        </div>
      }
    >
      <HubTabs tabs={tabs} label={t("hubTitle")} />
      <Outlet />
    </PageFrame>
  );
}
