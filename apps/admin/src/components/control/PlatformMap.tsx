import { useMemo } from "react";
import { Link } from "react-router-dom";
import type { ServiceHealthItem } from "@/api/types";
import { badgeClass, toneFromStatus } from "@/components/control/tones";
import { ExternalToolLink } from "@/platform/tools";
import {
  getTopologyNode,
  neighborsOf,
  TOPOLOGY_EDGES,
  TOPOLOGY_NODES,
  type TopologyNode,
  type TopologyNodeKind,
} from "@/platform/topology";

export type MapHealthLookup = Map<string, ServiceHealthItem>;

function healthFor(node: TopologyNode, health: MapHealthLookup): ServiceHealthItem | undefined {
  if (!node.healthService) return undefined;
  return health.get(node.healthService);
}

function statusForNode(
  node: TopologyNode,
  health: MapHealthLookup,
): { status?: string; reachable?: boolean } {
  const live = healthFor(node, health);
  if (live) return { status: live.status, reachable: live.reachable };
  if (node.kind === "edge" && health.size > 0) return { status: "Healthy", reachable: true };
  if (node.kind === "broker" || node.kind === "cache" || node.kind === "database") {
    return { status: "Infra", reachable: undefined };
  }
  if (node.kind === "observability" || node.kind === "console") {
    return { status: "Ready", reachable: true };
  }
  if (node.healthService) return { status: "Unknown", reachable: false };
  return { status: "Optional", reachable: undefined };
}

function Dot({ status, reachable }: { status?: string; reachable?: boolean }) {
  const tone = toneFromStatus(status, reachable);
  const color =
    tone === "healthy"
      ? "var(--msf-healthy)"
      : tone === "degraded"
        ? "var(--msf-degraded)"
        : tone === "critical"
          ? "var(--msf-critical)"
          : "var(--msf-infra)";
  return (
    <span
      style={{
        display: "inline-block",
        width: 8,
        height: 8,
        borderRadius: "50%",
        background: color,
      }}
    />
  );
}

const LAYER_GAP = 110;
const NODE_W = 132;
const NODE_H = 56;
const PAD_X = 48;
const PAD_Y = 36;

function layoutNodes(nodes: TopologyNode[]) {
  const layers = new Map<number, TopologyNode[]>();
  for (const node of nodes) {
    const list = layers.get(node.layer) ?? [];
    list.push(node);
    layers.set(node.layer, list);
  }
  const positions = new Map<string, { x: number; y: number }>();
  let maxWidth = 0;
  const sortedLayers = [...layers.keys()].sort((a, b) => a - b);
  for (const layer of sortedLayers) {
    const row = layers.get(layer)!;
    const rowWidth = row.length * NODE_W + (row.length - 1) * 24;
    maxWidth = Math.max(maxWidth, rowWidth);
  }
  const canvasWidth = Math.max(maxWidth + PAD_X * 2, 720);
  for (const layer of sortedLayers) {
    const row = layers.get(layer)!;
    const rowWidth = row.length * NODE_W + (row.length - 1) * 24;
    let x = (canvasWidth - rowWidth) / 2;
    const y = PAD_Y + layer * LAYER_GAP;
    for (const node of row) {
      positions.set(node.id, { x, y });
      x += NODE_W + 24;
    }
  }
  const canvasHeight = PAD_Y * 2 + Math.max(sortedLayers.length - 1, 0) * LAYER_GAP + NODE_H;
  return { positions, canvasWidth, canvasHeight };
}

function kindClass(kind: TopologyNodeKind): string {
  switch (kind) {
    case "edge":
      return "is-edge";
    case "service":
      return "is-service";
    case "broker":
      return "is-messaging";
    case "cache":
    case "database":
      return "is-infra";
    case "observability":
      return "is-obs";
    default:
      return "is-console";
  }
}

export function DependencyGraph({
  health,
  selectedId,
  onSelect,
  focusRelated = true,
}: {
  health: MapHealthLookup;
  selectedId: string | null;
  onSelect: (id: string) => void;
  focusRelated?: boolean;
}) {
  const { positions, canvasWidth, canvasHeight } = useMemo(() => layoutNodes(TOPOLOGY_NODES), []);

  const related = useMemo(() => {
    if (!selectedId || !focusRelated) return new Set<string>();
    const { upstream, downstream } = neighborsOf(selectedId);
    return new Set([selectedId, ...upstream, ...downstream]);
  }, [selectedId, focusRelated]);

  return (
    <div className="msf-dep-graph" role="img" aria-label="Platform dependency graph">
      <svg
        className="msf-dep-graph__svg"
        viewBox={`0 0 ${canvasWidth} ${canvasHeight}`}
        width="100%"
        preserveAspectRatio="xMidYMin meet"
      >
        <defs>
          <marker id="msf-arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
            <path d="M0,0 L6,3 L0,6 Z" fill="currentColor" />
          </marker>
        </defs>
        {TOPOLOGY_EDGES.map((edge) => {
          const from = positions.get(edge.from);
          const to = positions.get(edge.to);
          if (!from || !to) return null;
          const x1 = from.x + NODE_W / 2;
          const y1 = from.y + NODE_H;
          const x2 = to.x + NODE_W / 2;
          const y2 = to.y;
          const midY = (y1 + y2) / 2;
          const active = !selectedId || related.has(edge.from) || related.has(edge.to);
          return (
            <g key={edge.id} className={active ? "msf-dep-edge is-active" : "msf-dep-edge is-dim"}>
              <path
                d={`M ${x1} ${y1} C ${x1} ${midY}, ${x2} ${midY}, ${x2} ${y2}`}
                fill="none"
                stroke="currentColor"
                strokeWidth={active ? 1.75 : 1}
                markerEnd="url(#msf-arrow)"
              />
              {edge.label ? (
                <text x={(x1 + x2) / 2} y={midY - 4} textAnchor="middle" className="msf-dep-edge__label">
                  {edge.label}
                </text>
              ) : null}
            </g>
          );
        })}
        {TOPOLOGY_NODES.map((node) => {
          const pos = positions.get(node.id)!;
          const { status, reachable } = statusForNode(node, health);
          const selected = selectedId === node.id;
          const dim = selectedId != null && focusRelated && !related.has(node.id);
          return (
            <g
              key={node.id}
              className={[
                "msf-dep-node",
                kindClass(node.kind),
                selected ? "is-selected" : "",
                dim ? "is-dim" : "",
              ]
                .filter(Boolean)
                .join(" ")}
              transform={`translate(${pos.x}, ${pos.y})`}
              role="button"
              tabIndex={0}
              onClick={() => onSelect(node.id)}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault();
                  onSelect(node.id);
                }
              }}
            >
              <title>{`${node.label}: ${status ?? "unknown"}`}</title>
              <rect width={NODE_W} height={NODE_H} rx={8} ry={8} />
              <foreignObject x={8} y={8} width={NODE_W - 16} height={NODE_H - 16}>
                <div className="msf-dep-node__body">
                  <div className="msf-dep-node__top">
                    <Dot status={status} reachable={reachable} />
                    <span className="msf-dep-node__kind">{node.kind}</span>
                  </div>
                  <div className="msf-dep-node__label">{node.label}</div>
                </div>
              </foreignObject>
            </g>
          );
        })}
      </svg>
    </div>
  );
}

export function MapNodeInspector({
  nodeId,
  health,
  onClose,
}: {
  nodeId: string | null;
  health: MapHealthLookup;
  onClose: () => void;
}) {
  if (!nodeId) {
    return (
      <div className="card msf-map-inspector">
        <div className="card-body text-secondary">
          Select a node on the map to inspect health, dependencies, and deep links.
        </div>
      </div>
    );
  }

  const node = getTopologyNode(nodeId);
  if (!node) return null;

  const live = healthFor(node, health);
  const { status, reachable } = statusForNode(node, health);
  const { upstream, downstream } = neighborsOf(node.id);
  const tone = toneFromStatus(status, reachable);

  return (
    <div className="card msf-map-inspector">
      <div className="card-header">
        <h3 className="card-title d-flex align-items-center gap-2 mb-0">
          <Dot status={status} reachable={reachable} />
          {node.label}
        </h3>
        <div className="card-actions">
          <span className={badgeClass(tone)}>{status ?? "Unknown"}</span>
          <button type="button" className="btn btn-sm ms-2" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
      <div className="card-body">
        <p className="text-secondary">{node.summary}</p>
        <div className="datagrid">
          <div className="datagrid-item">
            <div className="datagrid-title">Kind</div>
            <div className="datagrid-content text-capitalize">{node.kind}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Version / label</div>
            <div className="datagrid-content msf-mono">{node.versionLabel ?? "—"}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Health probe</div>
            <div className="datagrid-content">
              {live ? (
                <>
                  {live.status}
                  {live.durationMs != null ? ` · ${live.durationMs} ms` : ""}
                  {live.description ? ` — ${live.description}` : ""}
                </>
              ) : node.healthService ? (
                "No probe in current aggregate"
              ) : (
                "No cluster probe (infra / console)"
              )}
            </div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Metrics</div>
            <div className="datagrid-content text-secondary">
              {node.metricsHint ?? "Preview — awaiting metrics API"}
            </div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Upstream</div>
            <div className="datagrid-content">
              {upstream.length === 0
                ? "—"
                : upstream.map((id) => getTopologyNode(id)?.label ?? id).join(", ")}
            </div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Downstream</div>
            <div className="datagrid-content">
              {downstream.length === 0
                ? "—"
                : downstream.map((id) => getTopologyNode(id)?.label ?? id).join(", ")}
            </div>
          </div>
        </div>

        {node.kind === "service" && node.healthService && live && !live.reachable ? (
          <div className="alert alert-azure mt-3 mb-0" role="status">
            Service may be offline or require Compose profile <code>full</code>.
          </div>
        ) : null}

        <div className="btn-list mt-3">
          {node.adminPath ? (
            <Link className="btn btn-primary" to={node.adminPath}>
              Open in console
            </Link>
          ) : null}
          {node.openApiPath ? (
            <a className="btn" href={node.openApiPath} target="_blank" rel="noreferrer">
              OpenAPI
            </a>
          ) : null}
          {node.kind === "observability" ? (
            <>
              <ExternalToolLink id="seq" className="btn" />
              <ExternalToolLink id="jaeger" className="btn" />
              <ExternalToolLink id="grafana" className="btn" />
            </>
          ) : null}
          {node.id === "rabbitmq" ? <ExternalToolLink id="rabbitmq" className="btn" /> : null}
          {node.healthService ? (
            <Link className="btn" to={`/services/${node.id}`}>
              Service Center
            </Link>
          ) : null}
        </div>
      </div>
    </div>
  );
}

export function buildHealthLookup(items: ServiceHealthItem[]): MapHealthLookup {
  return new Map(items.map((item) => [item.service, item]));
}
