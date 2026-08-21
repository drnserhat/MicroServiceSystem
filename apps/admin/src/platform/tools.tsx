export type ExternalTool = {
  id: string;
  name: string;
  url: string;
  summary: string;
  note?: string;
};

/** Single source for observability + infra management deep links (Compose local SoT). */
export const EXTERNAL_TOOLS: ExternalTool[] = [
  {
    id: "seq",
    name: "Seq",
    url: "http://localhost:5341",
    summary: "Structured logs",
    note: "profile obs / full",
  },
  {
    id: "jaeger",
    name: "Jaeger",
    url: "http://localhost:16686",
    summary: "Distributed traces",
    note: "profile obs / full",
  },
  {
    id: "grafana",
    name: "Grafana",
    url: "http://localhost:3000",
    summary: "Dashboards",
    note: "profile obs / full · admin / admin",
  },
  {
    id: "prometheus",
    name: "Prometheus",
    url: "http://localhost:9090",
    summary: "Metrics scrape",
    note: "profile obs / full",
  },
  {
    id: "rabbitmq",
    name: "RabbitMQ",
    url: "http://localhost:15672",
    summary: "Broker management UI",
    note: "msf / msf (dev)",
  },
  {
    id: "redisinsight",
    name: "Redis Insight",
    url: "http://localhost:5540",
    summary: "Browse Redis keys and values",
    note: "profile tools · host redis:6379",
  },
  {
    id: "pgadmin",
    name: "pgAdmin",
    url: "http://localhost:5050",
    summary: "PostgreSQL browser",
    note: "profile tools · admin@example.com / admin",
  },
  {
    id: "mongoexpress",
    name: "Mongo Express",
    url: "http://localhost:8081",
    summary: "MongoDB browser",
    note: "profile full",
  },
];

export function getExternalTool(id: string): ExternalTool | undefined {
  return EXTERNAL_TOOLS.find((tool) => tool.id === id);
}

export function ExternalToolLink({
  id,
  className = "btn btn-sm",
  children,
}: {
  id: string;
  className?: string;
  children?: React.ReactNode;
}) {
  const tool = getExternalTool(id);
  if (!tool) return null;
  return (
    <a className={className} href={tool.url} target="_blank" rel="noreferrer" title={tool.summary}>
      {children ?? tool.name}
    </a>
  );
}

/** Optional tool UIs omitted from the default card grid (lite Admin does not run them). */
const DEFAULT_CARD_EXCLUDE = new Set([
  "rabbitmq",
  "redisinsight",
  "pgadmin",
  "mongoexpress",
  "seq",
  "jaeger",
  "grafana",
  "prometheus",
]);

export function ExternalToolCards({ ids }: { ids?: string[] }) {
  const tools = ids
    ? EXTERNAL_TOOLS.filter((tool) => ids.includes(tool.id))
    : EXTERNAL_TOOLS.filter((tool) => !DEFAULT_CARD_EXCLUDE.has(tool.id));

  return (
    <div className="row row-cards">
      {tools.map((tool) => (
        <div className="col-md-3" key={tool.id}>
          <div className="card msf-tool-card h-100">
            <div className="card-body">
              {tool.note ? <div className="subheader">{tool.note}</div> : null}
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
  );
}
