import { Link } from "react-router-dom";
import { HealthIndicator, PreviewBanner, SectionHeader } from "@/components/control";

type Node = { id: string; label: string; meta: string; to?: string };

const ROWS: Node[][] = [
  [{ id: "gateway", label: "Gateway", meta: "YARP + JWT + /ops", to: "/health" }],
  [
    { id: "identity", label: "Identity", meta: "Auth · tenants", to: "/tenants" },
    { id: "user", label: "User", meta: "Profiles", to: "/users" },
    { id: "coordinator", label: "Coordinator", meta: "Sagas", to: "/workflows" },
    { id: "settings", label: "Settings", meta: "KV CRUD", to: "/settings" },
  ],
  [
    { id: "audit", label: "Audit", meta: "full", to: "/audit" },
    { id: "logging", label: "Logging", meta: "full", to: "/logs" },
    { id: "notification", label: "Notification", meta: "full", to: "/notifications" },
    { id: "file", label: "File", meta: "full", to: "/files" },
    { id: "location", label: "Location", meta: "full", to: "/countries" },
  ],
  [
    { id: "rabbit", label: "RabbitMQ", meta: "Events", to: "/messaging" },
    { id: "redis", label: "Redis", meta: "Cache" },
    { id: "pg", label: "PostgreSQL", meta: "EF Core" },
    { id: "mongo", label: "MongoDB", meta: "Logging" },
  ],
];

export function ArchitecturePage() {
  return (
    <>
      <SectionHeader
        title="Architecture Explorer"
        description="Framework topology — Gateway edge, bounded contexts, broker, and data stores."
        actions={
          <Link className="btn" to="/building-blocks">
            BuildingBlocks
          </Link>
        }
      />
      <PreviewBanner>
        Nodes are navigational. Dependency edges are conceptual; no live topology API.
      </PreviewBanner>

      <div className="msf-arch-canvas">
        {ROWS.map((row, rowIndex) => (
          <div key={rowIndex}>
            <div className="msf-arch-row">
              {row.map((node) => {
                const body = (
                  <div className="msf-arch-node">
                    <div className="d-flex justify-content-center mb-1">
                      <HealthIndicator status="Healthy" />
                    </div>
                    <div className="fw-bold">{node.label}</div>
                    <div className="text-secondary small">{node.meta}</div>
                  </div>
                );
                return node.to ? (
                  <Link key={node.id} to={node.to} className="text-reset text-decoration-none">
                    {body}
                  </Link>
                ) : (
                  <div key={node.id}>{body}</div>
                );
              })}
            </div>
            {rowIndex < ROWS.length - 1 ? (
              <div className="text-center text-secondary my-1" aria-hidden>
                ↓
              </div>
            ) : null}
          </div>
        ))}
      </div>
    </>
  );
}
