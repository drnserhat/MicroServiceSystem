import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
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
  const { t } = useTranslation(["home", "common", "ops"]);
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
        setError(err instanceof ApiClientError ? err.message : t("loadFailed"));
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
      label: t("ops:gateway"),
      status: health.length ? t("healthy") : undefined,
      reachable: health.length > 0,
      hint: health.length ? "YARP · /ops aggregate" : "Awaiting ops.health.read",
      to: "/map",
    },
    {
      id: "identity",
      label: t("ops:identity"),
      status: identity?.status,
      reachable: identity?.reachable,
      hint: identity?.description ?? undefined,
      to: "/tenants",
    },
    {
      id: "user",
      label: t("ops:user"),
      status: user?.status,
      reachable: user?.reachable,
      hint: user?.description ?? undefined,
      to: "/users",
    },
    {
      id: "coordinator",
      label: t("ops:coordinator"),
      status: coordinator?.status,
      reachable: coordinator?.reachable,
      hint: coordinator?.description ?? undefined,
      to: "/workflows",
    },
    {
      id: "settings",
      label: t("ops:settings"),
      status: settings?.status,
      reachable: settings?.reachable,
      hint: settings?.description ?? undefined,
      to: "/settings",
    },
    {
      id: "rabbitmq",
      label: t("ops:rabbitmq"),
      status: t("ops:infra"),
      hint: "Compose lite · :15672",
      to: "/messaging",
    },
    {
      id: "redis",
      label: t("ops:redis"),
      status: t("ops:infra"),
      hint: "Compose lite always-on",
    },
    {
      id: "postgres",
      label: t("ops:postgres"),
      status: t("ops:infra"),
      hint: "Compose lite always-on",
    },
    {
      id: "mongo",
      label: t("ops:mongo"),
      status: t("ops:optional"),
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
    ? t("updated", { time: updatedAt.toLocaleTimeString() })
    : checkedAt
      ? t("healthChecked", { time: new Date(checkedAt).toLocaleTimeString() })
      : t("notRefreshed");

  return (
    <PageFrame
      pretitle={t("pretitle")}
      title={t("title")}
      description={t("description")}
      actions={
        <div className="btn-list align-items-center">
          <span className="text-secondary small d-none d-md-inline">{lastUpdatedLabel}</span>
          <button
            type="button"
            className="btn"
            disabled={loading || refreshing}
            onClick={() => void load("refresh")}
          >
            {refreshing ? t("common:refreshing") : t("common:refresh")}
          </button>
          <Link className="btn btn-primary" to="/map">
            {t("openPlatformMap")}
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
              label={t("healthy")}
              value={health.length ? counts.healthy : "—"}
              hint={health.length ? t("ofProbes", { count: health.length }) : t("noHealthData")}
              tone="healthy"
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label={t("warning")}
              value={health.length ? counts.warning : "—"}
              hint={t("degradedProbes")}
              tone={counts.warning > 0 ? "degraded" : "healthy"}
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label={t("critical")}
              value={health.length ? counts.critical : "—"}
              hint={t("unreachableProbes")}
              tone={counts.critical > 0 ? "critical" : "healthy"}
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label={t("outboxPending")}
              value={outbox?.pendingCount ?? "—"}
              hint={outbox ? `${outbox.service} outbox` : t("needsOutboxRead")}
              tone="messaging"
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label={t("deadLetters")}
              value={outbox?.deadLetterCount ?? "—"}
              hint={t("identityDlq")}
              tone={(outbox?.deadLetterCount ?? 0) > 0 ? "critical" : "messaging"}
              action={
                <Link className="btn btn-sm" to="/messaging">
                  {t("common:open")}
                </Link>
              }
            />
          </div>
          <div className="col-6 col-xl-2">
            <MetricCard
              label={t("environment")}
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
          <h3 className="mb-0">{t("infrastructure")}</h3>
          <Link className="ms-auto small" to="/map">
            {t("openMap")}
          </Link>
        </div>
        {loading ? <Skeleton height={88} /> : <HealthSummaryStrip items={infraItems} />}
      </div>

      {/* 3. Messaging + Workflow snapshot */}
      <div className="row row-cards mb-3">
        <div className="col-lg-6">
          <div className="card h-100">
            <div className="card-header">
              <h3 className="card-title">{t("messagingSnapshot")}</h3>
              <div className="card-actions">
                <Link to="/messaging">{t("messagingCenter")}</Link>
              </div>
            </div>
            <div className="card-body">
              <div className="datagrid">
                <div className="datagrid-item">
                  <div className="datagrid-title">{t("outboxPending")}</div>
                  <div className="datagrid-content">{outbox?.pendingCount ?? "—"}</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">{t("deadLetters")}</div>
                  <div className="datagrid-content">{outbox?.deadLetterCount ?? "—"}</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">{t("ops:rabbitmq")}</div>
                  <div className="datagrid-content">
                    <ExternalToolLink id="rabbitmq" className="btn btn-sm" />
                  </div>
                </div>
              </div>
              <p className="text-secondary small mb-0 mt-3">{t("messagingNote")}</p>
            </div>
          </div>
        </div>
        <div className="col-lg-6">
          <div className="card h-100">
            <div className="card-header">
              <h3 className="card-title">{t("workflowSnapshot")}</h3>
              <div className="card-actions">
                <Link to="/workflows">{t("workflowCenter")}</Link>
              </div>
            </div>
            <div className="card-body">
              <div className="datagrid">
                <div className="datagrid-item">
                  <div className="datagrid-title">{t("coordinator")}</div>
                  <div className="datagrid-content">{coordinator?.status ?? "—"}</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">{t("runningSagas")}</div>
                  <div className="datagrid-content text-secondary">{t("runningSagasPreview")}</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">{t("referenceFlow")}</div>
                  <div className="datagrid-content">
                    <span className="msf-mono">RegisterUser</span>
                  </div>
                </div>
              </div>
              <div className="mt-3">
                <Link className="btn btn-sm" to="/users/register">
                  {t("startRegistration")}
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
              title={t("recentActivity")}
              items={activityItems}
              empty={t("activityEmpty")}
              actions={
                <div className="btn-list">
                  <Link to="/audit">{t("observability:audit")}</Link>
                  <Link to="/logs">{t("observability:logs")}</Link>
                </div>
              }
            />
          )}
        </div>
        <div className="col-lg-4">
          <div className="card h-100">
            <div className="card-header">
              <h3 className="card-title">{t("quickActions")}</h3>
            </div>
            <div className="list-group list-group-flush">
              <Link className="list-group-item list-group-item-action" to="/map">
                {t("openPlatformMap")}
              </Link>
              <Link className="list-group-item list-group-item-action" to="/services">
                {t("serviceCenter")}
              </Link>
              <Link className="list-group-item list-group-item-action" to="/messaging">
                {t("inspectOutboxDlq")}
              </Link>
              {can(FrameworkPermissions.RegistrationUsersCreate) ? (
                <Link className="list-group-item list-group-item-action" to="/users/register">
                  {t("registerUserSaga")}
                </Link>
              ) : null}
              <Link className="list-group-item list-group-item-action" to="/observability">
                {t("observabilityHub")}
              </Link>
              <div className="list-group-item">
                <div className="subheader mb-2">{t("deepLinks")}</div>
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

      <PreviewBanner>{t("previewBanner")}</PreviewBanner>
      <div className="row row-cards">
        <div className="col-md-3">
          <MetricCard label={t("runningSagas")} value="—" hint={t("common:preview")} tone="info" />
        </div>
        <div className="col-md-3">
          <MetricCard label={t("inboxPending")} value="—" hint={t("common:preview")} tone="messaging" />
        </div>
        <div className="col-md-3">
          <MetricCard label={t("avgApiLatency")} value="—" hint={t("common:preview")} tone="degraded" />
        </div>
        <div className="col-md-3">
          <MetricCard
            label={t("packageCatalog")}
            value={PLATFORM_PACKAGES.length}
            hint={t("staticInventory")}
            tone="infra"
          />
        </div>
      </div>
    </PageFrame>
  );
}
