import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { ApiClientError } from "@/api/client";
import { getHealthAggregate } from "@/api/ops";
import type { ServiceHealthItem } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import {
  buildHealthLookup,
  DependencyGraph,
  MapNodeInspector,
} from "@/components/control/PlatformMap";
import { PageFrame, PreviewBanner, Skeleton } from "@/components/control";
import { ErrorAlert } from "@/components/ui";
import { TOPOLOGY_NODES } from "@/platform/topology";

const INSPECTOR_MIN = 280;
const INSPECTOR_MAX_RATIO = 0.5;
const INSPECTOR_DEFAULT = 360;

export function PlatformMapPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.OpsHealthRead}>
      <PlatformMapInner />
    </RequirePermission>
  );
}

function PlatformMapInner() {
  const [items, setItems] = useState<ServiceHealthItem[]>([]);
  const [checkedAt, setCheckedAt] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>("gateway");
  const [inspectorWidth, setInspectorWidth] = useState(INSPECTOR_DEFAULT);
  const splitRef = useRef<HTMLDivElement>(null);

  const load = useCallback(async (mode: "initial" | "refresh" = "initial") => {
    if (mode === "refresh") setRefreshing(true);
    else setLoading(true);
    setError(null);
    try {
      const data = await getHealthAggregate();
      setItems(data.services);
      setCheckedAt(data.checkedAtUtc);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : "Failed to load health.");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    void load("initial");
  }, [load]);

  const health = useMemo(() => buildHealthLookup(items), [items]);
  const healthy = items.filter((i) => i.status.toLowerCase() === "healthy").length;
  const probed = TOPOLOGY_NODES.filter((n) => n.healthService).length;

  function onResizeStart(event: React.PointerEvent<HTMLDivElement>) {
    event.preventDefault();
    const split = splitRef.current;
    if (!split) return;
    const startX = event.clientX;
    const startWidth = inspectorWidth;
    const boundsWidth = split.getBoundingClientRect().width;

    function onMove(ev: PointerEvent) {
      const delta = startX - ev.clientX;
      const max = Math.floor(boundsWidth * INSPECTOR_MAX_RATIO);
      const next = Math.min(max, Math.max(INSPECTOR_MIN, startWidth + delta));
      setInspectorWidth(next);
    }

    function onUp() {
      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerup", onUp);
    }

    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", onUp);
  }

  return (
    <PageFrame
      pretitle="Platform"
      title="Platform Map"
      description="Interactive runtime topology — click a node to inspect health, dependencies, and deep links."
      actions={
        <div className="btn-list align-items-center">
          {checkedAt ? (
            <span className="text-secondary small d-none d-lg-inline">
              {healthy}/{items.length || "—"} healthy · {new Date(checkedAt).toLocaleTimeString()}
            </span>
          ) : null}
          <button
            type="button"
            className="btn"
            disabled={loading || refreshing}
            onClick={() => void load("refresh")}
          >
            {refreshing ? "Refreshing…" : "Refresh"}
          </button>
          <Link className="btn" to="/services">
            Services
          </Link>
          <Link className="btn" to="/architecture">
            Design-time
          </Link>
        </div>
      }
    >
      <PreviewBanner>
        Topology and edges are a static catalog. Live health overlays gateway probes where they exist. Metrics and
        restart remain awaiting API.
      </PreviewBanner>

      <ErrorAlert error={error} />

      <div className="msf-split d-none d-xl-flex" ref={splitRef}>
        <div className="msf-split__main">
          <div className="card h-100">
            <div className="card-header">
              <h3 className="card-title">Runtime graph</h3>
              <div className="card-actions text-secondary small">
                {TOPOLOGY_NODES.length} nodes · {probed} probe-backed · drag handle to resize
              </div>
            </div>
            <div className="card-body p-2 p-md-3">
              {loading ? (
                <Skeleton height={420} />
              ) : (
                <DependencyGraph health={health} selectedId={selectedId} onSelect={setSelectedId} />
              )}
            </div>
          </div>
        </div>
        <div
          className="msf-split__handle"
          role="separator"
          aria-orientation="vertical"
          aria-label="Resize inspector"
          tabIndex={0}
          onPointerDown={onResizeStart}
          onKeyDown={(e) => {
            if (e.key === "ArrowLeft") setInspectorWidth((w) => Math.min(w + 24, 560));
            if (e.key === "ArrowRight") setInspectorWidth((w) => Math.max(w - 24, INSPECTOR_MIN));
          }}
        />
        <div className="msf-split__side" style={{ width: inspectorWidth }}>
          <MapNodeInspector
            nodeId={selectedId}
            health={health}
            onClose={() => setSelectedId(null)}
          />
        </div>
      </div>

      <div className="row g-3 d-xl-none">
        <div className="col-12">
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Runtime graph</h3>
            </div>
            <div className="card-body p-2">
              {loading ? (
                <Skeleton height={320} />
              ) : (
                <DependencyGraph health={health} selectedId={selectedId} onSelect={setSelectedId} />
              )}
            </div>
          </div>
        </div>
        <div className="col-12">
          <MapNodeInspector
            nodeId={selectedId}
            health={health}
            onClose={() => setSelectedId(null)}
          />
        </div>
      </div>
    </PageFrame>
  );
}
