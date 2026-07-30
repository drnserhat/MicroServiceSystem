import { useMemo, useState } from "react";
import { PreviewBanner, SectionHeader, StatusBadge } from "@/components/control";

type Block = {
  id: string;
  name: string;
  summary: string;
  usedBy: string[];
  version: string;
};

const BLOCKS: Block[] = [
  {
    id: "authentication",
    name: "Authentication",
    summary: "JWT + internal API key schemes for service-to-service calls.",
    usedBy: ["Gateway", "All APIs"],
    version: "BuildingBlocks",
  },
  {
    id: "authorization",
    name: "Authorization",
    summary: "Permission policies ([HasPermission]) over JWT claims.",
    usedBy: ["All APIs", "Admin SPA"],
    version: "BuildingBlocks",
  },
  {
    id: "caching",
    name: "Caching",
    summary: "Memory/Redis cache abstractions with tenant-aware keys.",
    usedBy: ["Gateway", "Services"],
    version: "BuildingBlocks",
  },
  {
    id: "persistence",
    name: "Persistence",
    summary: "EF Core base context, outbox/inbox stores, repositories.",
    usedBy: ["Identity", "User", "Settings", "…"],
    version: "BuildingBlocks",
  },
  {
    id: "messaging",
    name: "Messaging",
    summary: "RabbitMQ topology, publisher, consumers, outbox relay.",
    usedBy: ["Coordinator", "All event publishers"],
    version: "BuildingBlocks",
  },
  {
    id: "saga",
    name: "Saga",
    summary: "Orchestration runner, checkpoints, lease recovery.",
    usedBy: ["Coordinator"],
    version: "BuildingBlocks",
  },
  {
    id: "opentelemetry",
    name: "OpenTelemetry",
    summary: "Traces/metrics export to collector / Jaeger.",
    usedBy: ["All hosts"],
    version: "BuildingBlocks",
  },
  {
    id: "sharedkernel",
    name: "SharedKernel",
    summary: "Result, pagination, specifications, domain primitives.",
    usedBy: ["Every layer"],
    version: "Shared",
  },
  {
    id: "contracts",
    name: "Contracts",
    summary: "Integration event contracts across bounded contexts.",
    usedBy: ["Publishers/consumers"],
    version: "Shared",
  },
];

export function BuildingBlocksPage() {
  const [selectedId, setSelectedId] = useState(BLOCKS[0]!.id);
  const selected = useMemo(() => BLOCKS.find((b) => b.id === selectedId) ?? BLOCKS[0]!, [selectedId]);

  return (
    <>
      <SectionHeader
        title="BuildingBlocks Explorer"
        description="Cross-cutting technical capabilities — static catalog mirroring the repository."
      />
      <PreviewBanner>No versioned package registry API; metadata is maintained in the admin SPA.</PreviewBanner>

      <div className="row">
        <div className="col-md-4">
          <div className="list-group list-group-transparent mb-3">
            {BLOCKS.map((block) => (
              <button
                key={block.id}
                type="button"
                className={`list-group-item list-group-item-action ${selectedId === block.id ? "active" : ""}`}
                onClick={() => setSelectedId(block.id)}
              >
                {block.name}
              </button>
            ))}
          </div>
        </div>
        <div className="col-md-8">
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">{selected.name}</h3>
              <div className="card-actions">
                <StatusBadge tone="infra">{selected.version}</StatusBadge>
              </div>
            </div>
            <div className="card-body">
              <p>{selected.summary}</p>
              <div className="subheader">Used by</div>
              <div className="d-flex flex-wrap gap-1">
                {selected.usedBy.map((item) => (
                  <span key={item} className="badge bg-blue-lt">
                    {item}
                  </span>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
