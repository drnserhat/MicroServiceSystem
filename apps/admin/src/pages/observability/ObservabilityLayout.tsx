import { useMemo } from "react";
import { Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { HubTabs, PageFrame } from "@/components/control";
import { ExternalToolLink } from "@/platform/tools";

export function ObservabilityLayout() {
  const { t } = useTranslation("observability");
  const tabs = useMemo(
    () => [
      { to: "/observability", label: t("overview"), end: true as const },
      { to: "/observability/metrics", label: t("metrics") },
      { to: "/observability/tracing", label: t("tracing") },
      { to: "/observability/logs", label: t("logs") },
      { to: "/observability/audit", label: t("audit") },
      { to: "/observability/errors", label: t("errors") },
      { to: "/observability/performance", label: t("performance") },
      { to: "/observability/otel", label: t("openTelemetry") },
      { to: "/observability/prometheus", label: t("prometheus") },
      { to: "/observability/correlation", label: t("correlation") },
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
          <ExternalToolLink id="seq" className="btn" />
          <ExternalToolLink id="jaeger" className="btn" />
          <ExternalToolLink id="grafana" className="btn btn-primary" />
        </div>
      }
    >
      <HubTabs tabs={tabs} label={t("hubTitle")} />
      <Outlet />
    </PageFrame>
  );
}
