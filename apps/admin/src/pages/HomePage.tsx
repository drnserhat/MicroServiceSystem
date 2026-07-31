import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { listAuditEntries } from "@/api/audit";
import { ApiClientError, isServiceUnavailable } from "@/api/client";
import { listLogs } from "@/api/logging";
import { getHealthAggregate, getOutboxSnapshot } from "@/api/ops";
import type { AuditEntry, OutboxSummary, ServiceHealthItem, SystemLog } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import {
  ActivityFeed,
  type ActivityFeedItem,
  HealthSummaryStrip,
  type HealthSummaryItem,
  MetricCard,
  PageFrame,
  PreviewBanner,
  Skeleton,
} from "@/components/control";
import { PLATFORM_PACKAGES } from "@/platform/catalog";
import { ExternalToolLink } from "@/platform/tools";

const ADMIN_VERSION = "0.1.0";

function findStatus(items: ServiceHealthItem[], id: string) {
  return items.find((item) => item.service === id);
}

function classifyHealth(items: ServiceHealthItem[]) {
  let healthy = 0;
  let warning = 0;
  let critical = 0;
  for (const item of items) {
    const s = item.status.toLowerCase();
    if (s === "healthy" || s === "ok") healthy += 1;
    else if (!item.reachable || s === "unhealthy" || s === "unreachable" || s === "critical") critical += 1;
    else warning += 1;
  }
  return { healthy, warning, critical };
}

export function HomePage() {
  const { can } = useAuth();
  const [health, setHealth] = useState<ServiceHealthItem[]>([]);
  const [checkedAt, setCheckedAt] = useState<string | null>(null);
  const [outbox, setOutbox] = useState<OutboxSummary | null>(null);
  const [audits, setAudits] = useState<AuditEntry[]>([]);
  const [logs, setLogs] = useState<SystemLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [updatedAt, setUpdatedAt] = useState<Date | null>(null);

  const load = useCallback(
    async (mode: "initial" | "refresh" = "initial") => {
      if (mode === "refresh") setRefreshing(true);
      else setLoading(true);
      setError(null);
      try {
        const tasks: Promise<void>[] = [];

        if (can(FrameworkPermissions.OpsHealthRead)) {
          tasks.push(
            getHealthAggregate().then((data) => {
              setHealth(data.services);
              setCheckedAt(data.checkedAtUtc);
            }),
          );
        }

        if (can(FrameworkPermissions.OpsOutboxRead)) {
          tasks.push(
            getOutboxSnapshot("identity", 5).then((data) => {
              setOutbox(data.summary);
            }),
          );
        }

        if (can(FrameworkPermissions.AuditEntriesRead)) {
          tasks.push(
            listAuditEntries(1, 8)
              .then((data) => setAudits([...data.items]))
              .catch((err) => {
                if (!isServiceUnavailable(err)) {
                  /* keep empty */
                }
              }),
          );
        }

        if (can(FrameworkPermissions.LoggingLogsRead)) {
          tasks.push(
            listLogs({ pageNumber: 1, pageSize: 8 })
              .then((data) => setLogs([...data.items]))
              .catch(() => undefined),
          );
        }

        await Promise.allSettled(tasks);
        setUpdatedAt(new Date());
      } catch (err) {
        setError(err instanceof ApiClientError ? err.message : "Failed to load overview.");
      } finally {
        setLoading(false);
        setRefreshing(false);
      }
    },
    [can],
  );

  useEffect(() => {
    void load("initial");
  }, [load]);

  const counts = useMemo(() => classifyHealth(health), [health]);

  const profile = useMemo(() => {
    const addonReachable = PLATFORM_PACKAGES.filter((p) => p.kind === "addon" && p.healthService).some((p) => {
      const h = findStatus(health, p.healthService!);
      return h?.reachable;
    });
    const obsReachable = PLATFORM_PACKAGES.filter((p) => p.kind === "observability").length > 0 && addonReachable;
    if (addonReachable && obsReachable) return "full+obs (inferred)";
    if (addonReachable) return "full (inferred)";
    return "lite";
  }, [health]);

  const identity = findStatus(health, "identity");
  const user = findStatus(health, "user");
  const coordinator = findStatus(health, "coordinator");
  const settings = findStatus(health, "settings");

  const infraItems: HealthSummaryItem[] = [
    {
      id: "gateway",
      label: "Gateway",
      status: health.length ? "Healthy" : undefined,
      reachable: health.length > 0,
      hint: health.length ? "YARP · /ops aggregate" : "Awaiting ops.health.read",
      to: "/map",
    },
    {
      id: "identity",
      label: "Identity",
      status: identity?.status,
      reachable: identity?.reachable,
      hint: identity?.description ?? undefined,
      to: "/tenants",
    },
    {
      id: "user",
      label: "User",
      status: user?.status,
      reachable: user?.reachable,
      hint: user?.description ?? undefined,
      to: "/users",
    },
    {
      id: "coordinator",
      label: "Coordinator",
      status: coordinator?.status,
      reachable: coordinator?.reachable,
      hint: coordinator?.description ?? undefined,
      to: "/workflows",
    },
    {
      id: "settings",
      label: "Settings",
      status: settings?.status,
      reachable: settings?.reachable,
      hint: settings?.description ?? undefined,
      to: "/settings",
    },
    {
      id: "rabbitmq",
      label: "RabbitMQ",
      status: "Infra",
      hint: "Compose lite · :15672",
      to: "/messaging",
    },
    {
      id: "redis",
      label: "Redis",
      status: "Infra",
      hint: "Compose lite always-on",
    },
    {
      id: "postgres",
      label: "PostgreSQL",
      status: "Infra",
      hint: "Compose lite always-on",
    },
    {
      id: "mongo",
      label: "MongoDB",
      status: "Optional",
      hint: "Profile full",
    },
  ];

  const activityItems: ActivityFeedItem[] = useMemo(() => {
    const fromAudit: ActivityFeedItem[] = audits.map((item) => ({
      id: `audit-${item.id}`,
      kind: "audit",
      title: item.action,
      meta: `${item.resourceType}/${item.resourceId}`,
      badge: "Audit",
      tone: "info",
      href: "/audit",
    }));
    const fromLogs: ActivityFeedItem[] = logs.map((item) => ({
      id: `log-${item.id}`,
      kind: "log",
      title: item.message,
      meta: [item.source, item.timestamp ? new Date(item.timestamp).toLocaleString() : null]
        .filter(Boolean)
        .join(" · "),
      badge: item.level,
      tone:
        item.level.toLowerCase() === "error" || item.level.toLowerCase() === "fatal"
          ? "critical"
          : item.level.toLowerCase() === "warning"
            ? "degraded"
            : "info",
      href: "/logs",
    }));
    return [...fromAudit, ...fromLogs].slice(0, 10);
  }, [audits, logs]);

  const lastUpdatedLabel = updatedAt
    ? `Updated ${updatedAt.toLocaleTimeString()}`
    : checkedAt
      ? `Health ${new Date(checkedAt).toLocaleTimeString()}`
      : "Not refreshed yet";

  return (
    <PageFrame
      pretitle="Platform"
      title="Platform Overview"
      description="Pulse of the MicroServiceSystem control plane — live probes where APIs exist."
      actions={
        <div className="btn-list align-items-center">
          <span className="text-secondary small d-none d-md-inline">{lastUpdatedLabel}</span>
          <button
            type="button"
            className="btn"
            disabled={loading || refreshing}
            onClick={() => void load("refresh")}
          >
            {refreshing ? "Refreshing…" : "Refresh"}
          </button>
          <Link className="btn btn-primary" to="/map">
            Platform Map
          </Link>
        </div>
      }
    >
      {error ? <div className="alert alert-danger">{error}</div> : null}

      {/* 1. Health KPI strip */}
      {loading ? (
        <div className="row row-cards mb-3">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div className="col-6 col-xl-2" key={i}>
              <Skeleton height={100} />
            </div>
          ))}
        </div>
      ) : (
        <div className="row row-cards mb-3">
          <div className="col-6 col-xl-2">
            <MetricCard
              label="Healthy"
              value={health.length ? counts.healthy : "—"}
              hint={health.length ? `of ${health.length} probes` : "No health data"}
              tone="healthy"
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label="Warning"
              value={health.length ? counts.warning : "—"}
              hint="Degraded probes"
              tone={counts.warning > 0 ? "degraded" : "healthy"}
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label="Critical"
              value={health.length ? counts.critical : "—"}
              hint="Unreachable / unhealthy"
              tone={counts.critical > 0 ? "critical" : "healthy"}
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label="Outbox pending"
              value={outbox?.pendingCount ?? "—"}
              hint={outbox ? `${outbox.service} outbox` : "Needs ops.outbox.read"}
              tone="messaging"
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label="Dead letters"
              value={outbox?.deadLetterCount ?? "—"}
              hint="Identity DLQ"
              tone={(outbox?.deadLetterCount ?? 0) > 0 ? "critical" : "messaging"}
              action={
                <Link className="btn btn-sm" to="/messaging">
                  Open
                </Link>
              }
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label="Environment"
              value={profile}
              hint={`Admin ${ADMIN_VERSION}`}
              tone="info"
            />
          </div>
        </div>
      )}

      {/* 2. Infra status row */}
      <div className="mb-3">
        <div className="d-flex align-items-center mb-2">
          <h3 className="mb-0">Infrastructure</h3>
          <Link className="ms-auto small" to="/map">
            Open map
          </Link>
        </div>
        {loading ? <Skeleton height={88} /> : <HealthSummaryStrip items={infraItems} />}
      </div>

      {/* 3. Messaging + Workflow snapshot */}
      <div className="row row-cards mb-3">
        <div className="col-lg-6">
          <div className="card h-100">
            <div className="card-header">
              <h3 className="card-title">Messaging snapshot</h3>
              <div className="card-actions">
                <Link to="/messaging">Messaging Center</Link>
              </div>
            </div>
            <div className="card-body">
              <div className="datagrid">
                <div className="datagrid-item">
                  <div className="datagrid-title">Pending</div>
                  <div className="datagrid-content">{outbox?.pendingCount ?? "—"}</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">Dead letters</div>
                  <div className="datagrid-content">{outbox?.deadLetterCount ?? "—"}</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">Broker</div>
                  <div className="datagrid-content">
                    <ExternalToolLink id="rabbitmq" className="btn btn-sm" />
                  </div>
                </div>
              </div>
              <p className="text-secondary small mb-0 mt-3">
                Live Identity outbox today. Multi-service outbox / inbox remain UI preview in Messaging Center.
              </p>
            </div>
          </div>
        </div>
        <div className="col-lg-6">
          <div className="card h-100">
            <div className="card-header">
              <h3 className="card-title">Workflow snapshot</h3>
              <div className="card-actions">
                <Link to="/workflows">Workflow Center</Link>
              </div>
            </div>
            <div className="card-body">
              <div className="datagrid">
                <div className="datagrid-item">
                  <div className="datagrid-title">Coordinator</div>
                  <div className="datagrid-content">{coordinator?.status ?? "—"}</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">Running sagas</div>
                  <div className="datagrid-content text-secondary">Preview — awaiting API</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">Reference flow</div>
                  <div className="datagrid-content">
                    <span className="msf-mono">RegisterUser</span>
                  </div>
                </div>
              </div>
              <div className="mt-3">
                <Link className="btn btn-sm" to="/users/register">
                  Start registration
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* 4. Activity feed + 5. Quick actions */}
      <div className="row row-cards mb-3">
        <div className="col-lg-8">
          {loading ? (
            <Skeleton height={280} />
          ) : (
            <ActivityFeed
              title="Recent activity"
              items={activityItems}
              empty="No audit/log activity (needs full profile + permissions)."
              actions={
                <div className="btn-list">
                  <Link to="/audit">Audit</Link>
                  <Link to="/logs">Logs</Link>
                </div>
              }
            />
          )}
        </div>
        <div className="col-lg-4">
          <div className="card h-100">
            <div className="card-header">
              <h3 className="card-title">Quick actions</h3>
            </div>
            <div className="list-group list-group-flush">
              <Link className="list-group-item list-group-item-action" to="/map">
                Open Platform Map
              </Link>
              <Link className="list-group-item list-group-item-action" to="/services">
                Service Center
              </Link>
              <Link className="list-group-item list-group-item-action" to="/messaging">
                Inspect outbox / DLQ
              </Link>
              {can(FrameworkPermissions.RegistrationUsersCreate) ? (
                <Link className="list-group-item list-group-item-action" to="/users/register">
                  Register user (saga)
                </Link>
              ) : null}
              <Link className="list-group-item list-group-item-action" to="/observability">
                Observability Hub
              </Link>
              <div className="list-group-item">
                <div className="subheader mb-2">Deep links</div>
                <div className="btn-list">
                  <ExternalToolLink id="seq" />
                  <ExternalToolLink id="jaeger" />
                  <ExternalToolLink id="grafana" />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <PreviewBanner>
        Running sagas, inbox pending, latency, queue delay, deployments, and container metrics are UI-only until
        ops APIs exist.
      </PreviewBanner>
      <div className="row row-cards">
        <div className="col-md-3">
          <MetricCard label="Running sagas" value="—" hint="Preview" tone="info" />
        </div>
        <div className="col-md-3">
          <MetricCard label="Inbox pending" value="—" hint="Preview" tone="messaging" />
        </div>
        <div className="col-md-3">
          <MetricCard label="Avg API latency" value="—" hint="Preview" tone="degraded" />
        </div>
        <div className="col-md-3">
          <MetricCard
            label="Package catalog"
            value={PLATFORM_PACKAGES.length}
            hint="Static inventory"
            tone="infra"
          />
        </div>
      </div>
    </PageFrame>
  );
}
