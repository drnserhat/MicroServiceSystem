import { Outlet } from "react-router-dom";
import { HubTabs, PageFrame } from "@/components/control";
import { ExternalToolLink } from "@/platform/tools";

const TABS = [
  { to: "/observability", label: "Overview", end: true },
  { to: "/observability/metrics", label: "Metrics" },
  { to: "/observability/tracing", label: "Tracing" },
  { to: "/observability/logs", label: "Logs" },
  { to: "/observability/audit", label: "Audit" },
  { to: "/observability/errors", label: "Errors" },
  { to: "/observability/performance", label: "Performance" },
  { to: "/observability/otel", label: "OTel" },
  { to: "/observability/prometheus", label: "Prometheus" },
  { to: "/observability/correlation", label: "Correlation" },
] as const;

export function ObservabilityLayout() {
  return (
    <PageFrame
      pretitle="Observability"
      title="Observability Center"
      description="In-product audit/logs plus deep links to Seq, Jaeger, Grafana, and Prometheus — not a replacement."
      actions={
        <div className="btn-list">
          <ExternalToolLink id="seq" className="btn" />
          <ExternalToolLink id="jaeger" className="btn" />
          <ExternalToolLink id="grafana" className="btn btn-primary" />
        </div>
      }
    >
      <HubTabs tabs={TABS} label="Observability sections" />
      <Outlet />
    </PageFrame>
  );
}
