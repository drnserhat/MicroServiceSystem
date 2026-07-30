import { Link } from "react-router-dom";
import { MetricCard, PreviewBanner, SectionHeader } from "@/components/control";

const TOOLS = [
  {
    name: "Seq",
    url: "http://localhost:5341",
    summary: "Structured logs",
    note: "profile obs / full",
  },
  {
    name: "Jaeger",
    url: "http://localhost:16686",
    summary: "Distributed traces",
    note: "OTLP from services",
  },
  {
    name: "Grafana",
    url: "http://localhost:3000",
    summary: "Dashboards",
    note: "admin / admin",
  },
  {
    name: "Prometheus",
    url: "http://localhost:9090",
    summary: "Metrics scrape",
    note: "/metrics on each service",
  },
];

export function ObservabilityPage() {
  return (
    <>
      <SectionHeader
        title="Observability Hub"
        description="OpenTelemetry pipeline destinations and in-product audit/log browsers."
      />
      <PreviewBanner>
        Latency/throughput charts require Prometheus queries — use Grafana until a metrics BFF exists.
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

      <div className="row row-cards mb-3">
        {TOOLS.map((tool) => (
          <div className="col-md-3" key={tool.name}>
            <div className="card">
              <div className="card-body">
                <div className="subheader">{tool.note}</div>
                <h3 className="card-title">{tool.name}</h3>
                <p className="text-secondary">{tool.summary}</p>
                <a className="btn" href={tool.url} target="_blank" rel="noreferrer">
                  Open
                </a>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="btn-list">
        <Link className="btn btn-primary" to="/audit">
          Audit browser
        </Link>
        <Link className="btn" to="/logs">
          System logs
        </Link>
        <Link className="btn" to="/health">
          Live health
        </Link>
      </div>
    </>
  );
}
