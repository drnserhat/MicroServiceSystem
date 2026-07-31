import { Link, useSearchParams } from "react-router-dom";
import { useState, type FormEvent } from "react";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import {
  MetricCard,
  MetricChartShell,
  PreviewBanner,
  StatusBadge,
} from "@/components/control";
import { ExternalToolCards, ExternalToolLink, getExternalTool } from "@/platform/tools";
import { AuditBrowser } from "@/pages/AuditPage";
import { LogsBrowser } from "@/pages/LogsPage";

export function ObservabilityOverviewPage() {
  return (
    <>
      <PreviewBanner>
        Latency/throughput charts require Prometheus queries — use Grafana until a metrics BFF exists.
        Audit and system logs below the hub tabs are live.
      </PreviewBanner>

      <div className="row row-cards mb-3">
        <div className="col-md-3">
          <MetricCard label="Availability" value="—" hint="Preview" tone="info" />
        </div>
        <div className="col-md-3">
          <MetricCard label="Error rate" value="—" hint="Preview" tone="critical" />
        </div>
        <div className="col-md-3">
          <MetricCard label="p95 latency" value="—" hint="Preview" tone="degraded" />
        </div>
        <div className="col-md-3">
          <MetricCard label="Throughput" value="—" hint="Preview" tone="info" />
        </div>
      </div>

      <div className="mb-3">
        <ExternalToolCards ids={["seq", "jaeger", "grafana", "prometheus"]} />
      </div>

      <div className="btn-list">
        <Link className="btn btn-primary" to="/observability/audit">
          Audit browser
        </Link>
        <Link className="btn" to="/observability/logs">
          System logs
        </Link>
        <Link className="btn" to="/observability/correlation">
          Correlation explorer
        </Link>
        <Link className="btn" to="/map">
          Platform Map
        </Link>
      </div>
    </>
  );
}

export function ObservabilityMetricsPage() {
  return (
    <>
      <PreviewBanner>
        Metrics stay in Prometheus/Grafana. This shell is journey UX only — no fabricated series.
      </PreviewBanner>
      <div className="row row-cards mb-3">
        <div className="col-md-6">
          <MetricChartShell title="Request rate" />
        </div>
        <div className="col-md-6">
          <MetricChartShell title="Error ratio" />
        </div>
      </div>
      <ExternalToolLink id="grafana" className="btn btn-primary" />
      <ExternalToolLink id="prometheus" className="btn ms-2" />
    </>
  );
}

export function ObservabilityTracingPage() {
  return (
    <>
      <PreviewBanner>Distributed traces live in Jaeger (OTLP from services). No in-console trace store.</PreviewBanner>
      <div className="card mb-3">
        <div className="card-body">
          <h3 className="card-title">Trace journey</h3>
          <p className="text-secondary">
            Gateway → service → outbox relay → consumer. Open Jaeger with the correlation id from
            logs or registration.
          </p>
          <ExternalToolLink id="jaeger" className="btn btn-primary" />
          <Link className="btn ms-2" to="/observability/correlation">
            Correlation explorer
          </Link>
        </div>
      </div>
    </>
  );
}

export function ObservabilityLogsPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.LoggingLogsRead}>
      <PreviewBanner>
        Live Logging service browser. Standalone alias remains at <code>/logs</code>. Prefer Seq for
        rich structured search.
      </PreviewBanner>
      <LogsBrowser />
    </RequirePermission>
  );
}

export function ObservabilityAuditPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.AuditEntriesRead}>
      <PreviewBanner>
        Live Audit entries. Standalone alias remains at <code>/audit</code>.
      </PreviewBanner>
      <AuditBrowser />
    </RequirePermission>
  );
}

export function ObservabilityErrorsPage() {
  return (
    <>
      <PreviewBanner>Error aggregation requires metrics/logs BFF — open Seq / Grafana for now.</PreviewBanner>
      <div className="row row-cards">
        <div className="col-md-4">
          <MetricCard label="5xx (preview)" value="—" tone="critical" />
        </div>
        <div className="col-md-4">
          <MetricCard label="Unhandled (preview)" value="—" tone="degraded" />
        </div>
        <div className="col-md-4">
          <MetricCard
            label="Deep links"
            value={
              <div className="btn-list">
                <ExternalToolLink id="seq" />
                <ExternalToolLink id="grafana" />
              </div>
            }
          />
        </div>
      </div>
    </>
  );
}

export function ObservabilityPerformancePage() {
  return (
    <>
      <PreviewBanner>p95/p99 and saturation stay in Grafana until a metrics BFF exists.</PreviewBanner>
      <div className="row row-cards mb-3">
        <div className="col-md-6">
          <MetricChartShell title="Latency percentiles" />
        </div>
        <div className="col-md-6">
          <MetricChartShell title="Saturation" />
        </div>
      </div>
      <ExternalToolLink id="grafana" className="btn btn-primary" />
    </>
  );
}

export function ObservabilityOtelPage() {
  return (
    <>
      <PreviewBanner>
        OpenTelemetry is the pipeline — exporters to Seq/Jaeger/Prometheus. This page documents the
        path; it does not host collectors.
      </PreviewBanner>
      <div className="card">
        <div className="card-body">
          <ol className="mb-3">
            <li>
              Services emit OTLP via <code>BuildingBlocks.OpenTelemetry</code>
            </li>
            <li>Traces → Jaeger · Logs → Seq · Metrics → Prometheus</li>
            <li>Dashboards → Grafana</li>
          </ol>
          <div className="btn-list">
            <StatusBadge tone="info">OTLP</StatusBadge>
            <ExternalToolLink id="jaeger" className="btn btn-sm" />
            <ExternalToolLink id="seq" className="btn btn-sm" />
            <ExternalToolLink id="prometheus" className="btn btn-sm" />
          </div>
        </div>
      </div>
    </>
  );
}

export function ObservabilityPrometheusPage() {
  return (
    <>
      <PreviewBanner>
        Each service exposes <code>/metrics</code>. Scrape config and PromQL stay in Prometheus —
        not replaced here.
      </PreviewBanner>
      <ExternalToolCards ids={["prometheus", "grafana"]} />
    </>
  );
}

export function ObservabilityCorrelationPage() {
  const [params, setParams] = useSearchParams();
  const [correlationId, setCorrelationId] = useState(() => params.get("q") ?? "");

  function onSearch(event: FormEvent) {
    event.preventDefault();
    const q = correlationId.trim();
    const next = new URLSearchParams(params);
    if (q) next.set("q", q);
    else next.delete("q");
    setParams(next, { replace: true });
  }

  const q = (params.get("q") ?? "").trim();
  const logsTo = q
    ? `/observability/logs?correlationId=${encodeURIComponent(q)}`
    : "/observability/logs";
  const jaeger = getExternalTool("jaeger");
  const seq = getExternalTool("seq");
  const jaegerHref = q && jaeger ? `${jaeger.url}/search?limit=20` : jaeger?.url;
  const seqHref = q && seq ? `${seq.url}/#/events?filter=${encodeURIComponent(`@CorrelationId = "${q}"`)}` : seq?.url;

  return (
    <>
      <PreviewBanner>
        Full join across logs + traces + audit awaits a correlation BFF. Logs filter below uses the live Logging
        API; Seq/Jaeger links open external tools with the id as a starting point.
      </PreviewBanner>
      <form className="card mb-3" onSubmit={onSearch}>
        <div className="card-header">
          <h3 className="card-title">Lookup</h3>
        </div>
        <div className="card-body">
          <div className="row g-2 align-items-end">
            <div className="col-md-8">
              <label className="form-label" htmlFor="corr-id">
                Correlation id
              </label>
              <input
                id="corr-id"
                className="form-control"
                placeholder="Paste correlation / trace id"
                value={correlationId}
                onChange={(e) => setCorrelationId(e.target.value)}
              />
            </div>
            <div className="col-md-4">
              <button type="submit" className="btn btn-primary w-100">
                Search
              </button>
            </div>
          </div>
        </div>
      </form>

      {q ? (
        <div className="card mb-3">
          <div className="card-body">
            <div className="subheader">Active correlation</div>
            <code className="d-block mb-3 text-break">{q}</code>
            <div className="btn-list">
              <Link className="btn btn-primary" to={logsTo}>
                Filter system logs
              </Link>
              <Link className="btn" to="/messaging/dead-letters">
                Messaging DLQ
              </Link>
              {jaegerHref ? (
                <a className="btn" href={jaegerHref} target="_blank" rel="noreferrer">
                  Open Jaeger
                </a>
              ) : null}
              {seqHref ? (
                <a className="btn" href={seqHref} target="_blank" rel="noreferrer">
                  Open Seq
                </a>
              ) : null}
            </div>
          </div>
        </div>
      ) : (
        <p className="text-secondary">Enter a correlation id to fan out to live logs and external tools.</p>
      )}
    </>
  );
}
