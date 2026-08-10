import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ApiClientError } from "@/api/client";
import {
  getHealthAggregate,
  getInboxSummary,
  getOutboxSnapshot,
  OUTBOX_SERVICES,
  type OutboxService,
} from "@/api/ops";
import type { InboxSummary, OutboxSummary, ServiceHealthItem } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { useAuth } from "@/auth/AuthContext";
import {
  EmptyState,
  HealthIndicator,
  PageFrame,
  PreviewBanner,
  ServiceCard,
  Skeleton,
  StatusBadge,
} from "@/components/control";
import { FilterBar, usePinnedServices } from "@/components/control/FilterBar";
import { PLATFORM_PACKAGES, type PlatformPackage, type PlatformPackageKind } from "@/platform/catalog";
import { ExternalToolLink } from "@/platform/tools";
import { getTopologyNode, neighborsOf } from "@/platform/topology";

type KindFilter = "all" | PlatformPackageKind | "pinned";
type StatusFilter = "all" | "healthy" | "attention" | "unknown";

const DETAIL_TABS = [
  { id: "overview", label: "Overview" },
  { id: "health", label: "Health" },
  { id: "dependencies", label: "Dependencies" },
  { id: "database", label: "Database" },
  { id: "redis", label: "Redis" },
  { id: "rabbitmq", label: "RabbitMQ" },
  { id: "outbox", label: "Outbox" },
  { id: "inbox", label: "Inbox" },
  { id: "metrics", label: "Metrics" },
  { id: "tracing", label: "Tracing" },
  { id: "logs", label: "Logs" },
  { id: "configuration", label: "Configuration" },
  { id: "environment", label: "Environment" },
  { id: "docker", label: "Docker" },
  { id: "openapi", label: "OpenAPI" },
  { id: "version", label: "Version" },
  { id: "deployment", label: "Deployment" },
  { id: "timeline", label: "Health Timeline" },
] as const;

type DetailTabId = (typeof DETAIL_TABS)[number]["id"];

function healthFor(pkg: PlatformPackage, items: ServiceHealthItem[]) {
  if (!pkg.healthService) return undefined;
  return items.find((i) => i.service === pkg.healthService);
}

function isAttention(live?: ServiceHealthItem) {
  if (!live) return false;
  const s = live.status.toLowerCase();
  return !live.reachable || (s !== "healthy" && s !== "ok");
}

export function ServicesPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.OpsHealthRead}>
      <ServicesInner />
    </RequirePermission>
  );
}

function ServicesInner() {
  const { t } = useTranslation(["platform", "common"]);
  const { serviceId } = useParams<{ serviceId?: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const { can } = useAuth();
  const { pins, pinnedSet, togglePin } = usePinnedServices();

  const [health, setHealth] = useState<ServiceHealthItem[]>([]);
  const [outboxByService, setOutboxByService] = useState<Partial<Record<OutboxService, OutboxSummary>>>({});
  const [inboxByService, setInboxByService] = useState<Partial<Record<OutboxService, InboxSummary>>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [kindFilter, setKindFilter] = useState<KindFilter>("all");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");

  const tabParam = (searchParams.get("tab") as DetailTabId | null) ?? "overview";
  const activeTab: DetailTabId = DETAIL_TABS.some((t) => t.id === tabParam) ? tabParam : "overview";

  const services = useMemo(
    () => PLATFORM_PACKAGES.filter((p) => p.healthService || ["gateway", "admin"].includes(p.id)),
    [],
  );

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const tasks: Promise<void>[] = [
          getHealthAggregate().then((data) => {
            if (!cancelled) setHealth(data.services);
          }),
        ];
        if (can(FrameworkPermissions.OpsOutboxRead)) {
          for (const service of OUTBOX_SERVICES) {
            tasks.push(
              getOutboxSnapshot(service, 5)
                .then((data) => {
                  if (!cancelled) {
                    setOutboxByService((prev) => ({ ...prev, [service]: data.summary }));
                  }
                })
                .catch(() => undefined),
            );
          }
        }
        if (can(FrameworkPermissions.OpsInboxRead)) {
          for (const service of OUTBOX_SERVICES) {
            tasks.push(
              getInboxSummary(service)
                .then((data) => {
                  if (!cancelled) {
                    setInboxByService((prev) => ({ ...prev, [service]: data }));
                  }
                })
                .catch(() => undefined),
            );
          }
        }
        await Promise.allSettled(tasks);
      } catch (err) {
        if (!cancelled) setError(err instanceof ApiClientError ? err.message : t("healthLoadFailed"));
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, [can]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return services
      .filter((pkg) => {
        if (kindFilter === "pinned") return pinnedSet.has(pkg.id);
        if (kindFilter !== "all" && pkg.kind !== kindFilter) return false;
        const live = healthFor(pkg, health);
        if (statusFilter === "healthy") {
          return live?.status.toLowerCase() === "healthy" || live?.status.toLowerCase() === "ok";
        }
        if (statusFilter === "attention") return isAttention(live);
        if (statusFilter === "unknown") return !live && Boolean(pkg.healthService);
        return true;
      })
      .filter((pkg) => {
        if (!q) return true;
        return `${pkg.name} ${pkg.id} ${pkg.summary} ${pkg.kind}`.toLowerCase().includes(q);
      })
      .sort((a, b) => {
        const ap = pinnedSet.has(a.id) ? 0 : 1;
        const bp = pinnedSet.has(b.id) ? 0 : 1;
        if (ap !== bp) return ap - bp;
        return a.name.localeCompare(b.name);
      });
  }, [services, kindFilter, statusFilter, search, health, pinnedSet]);

  const selected = serviceId ? services.find((p) => p.id === serviceId) : undefined;
  const selectedHealth = selected ? healthFor(selected, health) : undefined;
  const topo = selected ? getTopologyNode(selected.id) : undefined;

  function setTab(id: DetailTabId) {
    setSearchParams(id === "overview" ? {} : { tab: id }, { replace: true });
  }

  return (
    <PageFrame
      pretitle={t("pretitle")}
      title={selected ? selected.name : t("titleServiceCenter")}
      description={
        selected
          ? selected.summary
          : "Microservice inventory with live readiness, filters, pins, and a tabbed control-plane detail."
      }
      actions={
        <div className="btn-list">
          {selected ? (
            <Link className="btn" to="/services">
              {t("allServices")}
            </Link>
          ) : null}
          <Link className="btn" to="/map">
            {t("platformMap")}
          </Link>
          <Link className="btn" to="/platform">
            {t("packages")}
          </Link>
        </div>
      }
    >
      {error ? <div className="alert alert-danger">{error}</div> : null}

      {!selected ? (
        <>
          <FilterBar
            search={search}
            onSearchChange={setSearch}
            searchPlaceholder="Search services…"
            chips={[
              { id: "all", label: "All" },
              { id: "pinned", label: `Pinned (${pins.length})` },
              { id: "core", label: "Core" },
              { id: "addon", label: "Add-on" },
              { id: "observability", label: "Obs" },
            ]}
            activeChipId={kindFilter}
            onChipChange={(id) => setKindFilter(id as KindFilter)}
            trailing={
              <select
                className="form-select form-select-sm"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}
                aria-label="Status filter"
              >
                <option value="all">Any status</option>
                <option value="healthy">Healthy</option>
                <option value="attention">Attention</option>
                <option value="unknown">No probe</option>
              </select>
            }
          />

          {loading ? (
            <div className="row row-cards">
              {[1, 2, 3, 4, 5, 6].map((i) => (
                <div className="col-md-4" key={i}>
                  <Skeleton height={160} />
                </div>
              ))}
            </div>
          ) : filtered.length === 0 ? (
            <EmptyState title="No services match" description="Adjust search or filters." />
          ) : (
            <div className="row row-cards">
              {filtered.map((pkg) => {
                const live = healthFor(pkg, health);
                const pinned = pinnedSet.has(pkg.id);
                return (
                  <div className="col-md-4" key={pkg.id}>
                    <div
                      role="button"
                      tabIndex={0}
                      onClick={() => navigate(`/services/${pkg.id}`)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") navigate(`/services/${pkg.id}`);
                      }}
                    >
                      <ServiceCard
                        name={pkg.name}
                        summary={pkg.summary}
                        status={live?.status ?? (pkg.kind === "core" ? "Infra" : "Optional")}
                        reachable={live?.reachable}
                        kind={pkg.kind}
                        actions={
                          <div className="btn-list">
                            <button
                              type="button"
                              className="btn btn-sm"
                              title={pinned ? "Unpin" : "Pin"}
                              onClick={(e) => {
                                e.stopPropagation();
                                togglePin(pkg.id);
                              }}
                            >
                              {pinned ? "★" : "☆"}
                            </button>
                            {pkg.adminPath ? (
                              <Link
                                className="btn btn-sm"
                                to={pkg.adminPath}
                                onClick={(e) => e.stopPropagation()}
                              >
                                Open
                              </Link>
                            ) : null}
                            {pkg.gatewayPrefix && pkg.gatewayPrefix !== "/" ? (
                              <a
                                className="btn btn-sm"
                                href={`/docs/${pkg.id}/swagger.json`}
                                target="_blank"
                                rel="noreferrer"
                                onClick={(e) => e.stopPropagation()}
                              >
                                OpenAPI
                              </a>
                            ) : null}
                          </div>
                        }
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </>
      ) : (
        <ServiceDetail
          pkg={selected}
          live={selectedHealth}
          outbox={
            (OUTBOX_SERVICES as readonly string[]).includes(selected.id)
              ? (outboxByService[selected.id as OutboxService] ?? null)
              : null
          }
          inbox={
            (OUTBOX_SERVICES as readonly string[]).includes(selected.id)
              ? (inboxByService[selected.id as OutboxService] ?? null)
              : null
          }
          outboxService={
            (OUTBOX_SERVICES as readonly string[]).includes(selected.id)
              ? (selected.id as OutboxService)
              : null
          }
          pinned={pinnedSet.has(selected.id)}
          onTogglePin={() => togglePin(selected.id)}
          activeTab={activeTab}
          onTabChange={setTab}
          dependsOn={topo?.dependsOn ?? neighborsOf(selected.id).upstream}
          downstream={neighborsOf(selected.id).downstream}
        />
      )}
    </PageFrame>
  );
}

function ServiceDetail({
  pkg,
  live,
  outbox,
  inbox,
  outboxService,
  pinned,
  onTogglePin,
  activeTab,
  onTabChange,
  dependsOn,
  downstream,
}: {
  pkg: PlatformPackage;
  live?: ServiceHealthItem;
  outbox: OutboxSummary | null;
  inbox: InboxSummary | null;
  outboxService: OutboxService | null;
  pinned: boolean;
  onTogglePin: () => void;
  activeTab: DetailTabId;
  onTabChange: (id: DetailTabId) => void;
  dependsOn: string[];
  downstream: string[];
}) {
  const openApi =
    pkg.gatewayPrefix && pkg.gatewayPrefix !== "/"
      ? `/docs/${pkg.id}/swagger.json`
      : getTopologyNode(pkg.id)?.openApiPath;

  return (
    <>
      <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
        <HealthIndicator status={live?.status} reachable={live?.reachable} />
        <StatusBadge status={live?.status ?? pkg.kind} reachable={live?.reachable} />
        <span className="badge bg-secondary-lt text-uppercase">{pkg.kind}</span>
        <button type="button" className="btn btn-sm" onClick={onTogglePin}>
          {pinned ? "★ Pinned" : "☆ Pin"}
        </button>
        <button type="button" className="btn btn-sm" disabled title="No ops API">
          Restart
        </button>
        {pkg.adminPath ? (
          <Link className="btn btn-sm" to={pkg.adminPath}>
            Open console surface
          </Link>
        ) : null}
      </div>

      <ul className="nav nav-tabs mb-3 flex-nowrap overflow-auto">
        {DETAIL_TABS.map((tab) => (
          <li className="nav-item" key={tab.id}>
            <button
              type="button"
              className={`nav-link ${activeTab === tab.id ? "active" : ""}`}
              onClick={() => onTabChange(tab.id)}
            >
              {tab.label}
            </button>
          </li>
        ))}
      </ul>

      <div className="card">
        <div className="card-body">
          {renderTab(activeTab, {
            pkg,
            live,
            outbox,
            inbox,
            outboxService,
            openApi,
            dependsOn,
            downstream,
          })}
        </div>
      </div>
    </>
  );
}

function PreviewPanel({ title, children }: { title: string; children?: React.ReactNode }) {
  return (
    <>
      <PreviewBanner>{title} is UI architecture only until a dedicated ops API exists.</PreviewBanner>
      {children}
    </>
  );
}

function renderTab(
  tab: DetailTabId,
  ctx: {
    pkg: PlatformPackage;
    live?: ServiceHealthItem;
    outbox: OutboxSummary | null;
    inbox: InboxSummary | null;
    outboxService: OutboxService | null;
    openApi?: string;
    dependsOn: string[];
    downstream: string[];
  },
) {
  const { pkg, live, outbox, inbox, outboxService, openApi, dependsOn, downstream } = ctx;

  switch (tab) {
    case "overview":
      return (
        <div className="datagrid">
          <div className="datagrid-item">
            <div className="datagrid-title">Summary</div>
            <div className="datagrid-content">{pkg.summary}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Compose</div>
            <div className="datagrid-content">{pkg.composeNote}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Gateway prefix</div>
            <div className="datagrid-content">
              <code>{pkg.gatewayPrefix ?? "—"}</code>
            </div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Health service id</div>
            <div className="datagrid-content msf-mono">{pkg.healthService ?? "—"}</div>
          </div>
        </div>
      );
    case "health":
      return (
        <div className="datagrid">
          <div className="datagrid-item">
            <div className="datagrid-title">Status</div>
            <div className="datagrid-content">
              <StatusBadge status={live?.status ?? "Unknown"} reachable={live?.reachable} />
            </div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Reachable</div>
            <div className="datagrid-content">{live ? String(live.reachable) : "—"}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Duration</div>
            <div className="datagrid-content">
              {live?.durationMs != null ? `${live.durationMs} ms` : "—"}
            </div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Description</div>
            <div className="datagrid-content">{live?.description ?? "No probe in aggregate"}</div>
          </div>
        </div>
      );
    case "dependencies":
      return (
        <>
          <p className="text-secondary">From Platform Map topology catalog (static + navigable).</p>
          <div className="row">
            <div className="col-md-6">
              <h4>Upstream / depends on</h4>
              <ul className="list-unstyled">
                {dependsOn.length === 0 ? <li className="text-secondary">—</li> : null}
                {dependsOn.map((id) => (
                  <li key={id}>
                    <Link to={`/services/${id}`}>{getTopologyNode(id)?.label ?? id}</Link>
                  </li>
                ))}
              </ul>
            </div>
            <div className="col-md-6">
              <h4>Downstream</h4>
              <ul className="list-unstyled">
                {downstream.length === 0 ? <li className="text-secondary">—</li> : null}
                {downstream.map((id) => (
                  <li key={id}>
                    <Link to={`/map`}>{getTopologyNode(id)?.label ?? id}</Link>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </>
      );
    case "database":
      return (
        <div>
          <p className="text-secondary">
            Database-per-service. Expected store:{" "}
            <strong>{pkg.id === "logging" || pkg.id === "mongodb" ? "MongoDB" : "PostgreSQL"}</strong>{" "}
            (catalog heuristic). Browse data in the management UI — Admin does not proxy queries.
          </p>
          <div className="btn-list">
            {pkg.id === "logging" || pkg.id === "mongodb" ? (
              <ExternalToolLink id="mongoexpress" className="btn" />
            ) : (
              <ExternalToolLink id="pgadmin" className="btn" />
            )}
          </div>
        </div>
      );
    case "redis":
      return (
        <div>
          <p className="text-secondary">
            Cache and distributed primitives. Key/value inspection opens in Redis Insight (Compose),
            not inside this console.
          </p>
          <div className="btn-list">
            <ExternalToolLink id="redisinsight" className="btn" />
          </div>
        </div>
      );
    case "rabbitmq":
      return (
        <div>
          <p className="text-secondary mb-2">
            Broker topology and queue depths live in RabbitMQ Management. Outbox/inbox counts are
            Postgres-backed ops APIs in Messaging Center.
          </p>
          <div className="btn-list">
            <ExternalToolLink id="rabbitmq" className="btn" />
            <Link className="btn" to="/messaging">
              Messaging Center
            </Link>
          </div>
        </div>
      );
    case "outbox":
      return outboxService && outbox ? (
        <div className="datagrid">
          <div className="datagrid-item">
            <div className="datagrid-title">Service</div>
            <div className="datagrid-content">
              <code>{outbox.service}</code>
            </div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Pending</div>
            <div className="datagrid-content">{outbox.pendingCount}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Dead letters</div>
            <div className="datagrid-content">{outbox.deadLetterCount}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Actions</div>
            <div className="datagrid-content">
              <Link className="btn btn-sm" to={`/messaging/outbox?service=${outboxService}`}>
                Open Messaging Center
              </Link>
            </div>
          </div>
        </div>
      ) : (
        <PreviewPanel title="Per-service outbox">
          <p className="mb-0 text-secondary">
            This package has no outbox ops surface (or snapshot unavailable). Logging has no outbox store.
          </p>
        </PreviewPanel>
      );
    case "inbox":
      return outboxService && inbox ? (
        <div className="datagrid">
          <div className="datagrid-item">
            <div className="datagrid-title">Service</div>
            <div className="datagrid-content">
              <code>{inbox.service}</code>
            </div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Processed</div>
            <div className="datagrid-content">{inbox.processedCount}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Open</div>
            <div className="datagrid-content">{inbox.openCount}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">In flight</div>
            <div className="datagrid-content">{inbox.inFlightCount}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Failed</div>
            <div className="datagrid-content">{inbox.failedCount}</div>
          </div>
          <div className="datagrid-item">
            <div className="datagrid-title">Actions</div>
            <div className="datagrid-content">
              <Link className="btn btn-sm" to={`/messaging/inbox?service=${outboxService}`}>
                Open Messaging Center
              </Link>
            </div>
          </div>
        </div>
      ) : (
        <PreviewPanel title="Inbox / idempotent consumers">
          <p className="mb-0 text-secondary">
            {outboxService
              ? "Inbox summary unavailable (needs ops.inbox.read or service offline)."
              : "This package has no inbox ops surface."}
          </p>
          {outboxService ? (
            <div className="btn-list mt-2">
              <Link className="btn btn-sm" to={`/messaging/inbox?service=${outboxService}`}>
                Messaging Center
              </Link>
            </div>
          ) : null}
        </PreviewPanel>
      );
    case "metrics":
      return (
        <PreviewPanel title="CPU / memory / custom metrics">
          <div className="btn-list">
            <ExternalToolLink id="prometheus" className="btn" />
            <ExternalToolLink id="grafana" className="btn" />
          </div>
        </PreviewPanel>
      );
    case "tracing":
      return (
        <PreviewPanel title="Distributed traces">
          <ExternalToolLink id="jaeger" className="btn" />
        </PreviewPanel>
      );
    case "logs":
      return (
        <PreviewPanel title="Service-scoped logs">
          <div className="btn-list">
            <ExternalToolLink id="seq" className="btn" />
            <Link className="btn" to="/logs">
              System logs
            </Link>
          </div>
        </PreviewPanel>
      );
    case "configuration":
      return (
        <PreviewPanel title="Config / secrets">
          {pkg.id === "settings" ? (
            <Link className="btn" to="/settings">
              Tenant settings
            </Link>
          ) : (
            <p className="mb-0 text-secondary">No config browser API for this service.</p>
          )}
        </PreviewPanel>
      );
    case "environment":
      return (
        <PreviewPanel title="Environment variables">
          <p className="mb-0 text-secondary">Compose/env injection is managed outside this console.</p>
        </PreviewPanel>
      );
    case "docker":
      return (
        <PreviewPanel title="Container / Docker">
          <p className="mb-0 text-secondary">{pkg.composeNote}</p>
        </PreviewPanel>
      );
    case "openapi":
      return openApi ? (
        <div>
          <p className="text-secondary">Gateway-aggregated OpenAPI document.</p>
          <a className="btn btn-primary" href={openApi} target="_blank" rel="noreferrer">
            Open swagger.json
          </a>
        </div>
      ) : (
        <EmptyState title="No OpenAPI link" description="This package has no gateway API prefix." />
      );
    case "version":
      return (
        <PreviewPanel title="Build / image version">
          <p className="mb-0 msf-mono text-secondary">
            {getTopologyNode(pkg.id)?.versionLabel ?? pkg.kind} · awaiting version API
          </p>
        </PreviewPanel>
      );
    case "deployment":
      return (
        <PreviewPanel title="Deployment history">
          <p className="mb-0 text-secondary">Compose/Helm rollout history is not wired.</p>
        </PreviewPanel>
      );
    case "timeline":
      return (
        <PreviewPanel title="Health timeline">
          <p className="mb-0 text-secondary">Historical probe series require a metrics/BFF store.</p>
        </PreviewPanel>
      );
    default:
      return null;
  }
}
