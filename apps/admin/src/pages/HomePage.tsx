import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { listAuditEntries } from "@/api/audit";
import { ApiClientError, isServiceUnavailable } from "@/api/client";
import { listLogs } from "@/api/logging";
import { getHealthAggregate, getOutboxSnapshot } from "@/api/ops";
import type { AuditEntry, OutboxSummary, ServiceHealthItem, SystemLog } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import {
  HealthIndicator,
  MetricCard,
  PreviewBanner,
  SectionHeader,
  Skeleton,
  StatusBadge,
} from "@/components/control";
import { PLATFORM_PACKAGES } from "@/platform/catalog";

const ADMIN_VERSION = "0.1.0";

function findStatus(items: ServiceHealthItem[], id: string) {
  return items.find((item) => item.service === id);
}

export function HomePage() {
  const { can } = useAuth();
  const [health, setHealth] = useState<ServiceHealthItem[]>([]);
  const [outbox, setOutbox] = useState<OutboxSummary | null>(null);
  const [audits, setAudits] = useState<AuditEntry[]>([]);
  const [logs, setLogs] = useState<SystemLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const tasks: Promise<void>[] = [];

        if (can(FrameworkPermissions.OpsHealthRead)) {
          tasks.push(
            getHealthAggregate().then((data) => {
              if (!cancelled) setHealth(data.services);
            }),
          );
        }

        if (can(FrameworkPermissions.OpsOutboxRead)) {
          tasks.push(
            getOutboxSnapshot(5).then((data) => {
              if (!cancelled) setOutbox(data.summary);
            }),
          );
        }

        if (can(FrameworkPermissions.AuditEntriesRead)) {
          tasks.push(
            listAuditEntries(1, 5)
              .then((data) => {
                if (!cancelled) setAudits([...data.items]);
              })
              .catch((err) => {
                if (!cancelled && !isServiceUnavailable(err)) {
                  /* keep empty */
                }
              }),
          );
        }

        if (can(FrameworkPermissions.LoggingLogsRead)) {
          tasks.push(
            listLogs({ pageNumber: 1, pageSize: 5 })
              .then((data) => {
                if (!cancelled) setLogs([...data.items]);
              })
              .catch(() => undefined),
          );
        }

        await Promise.allSettled(tasks);
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiClientError ? err.message : "Failed to load overview.");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, [can]);

  const healthy = health.filter((s) => s.status.toLowerCase() === "healthy").length;
  const unhealthy = health.filter((s) => s.status.toLowerCase() !== "healthy").length;
  const profile = useMemo(() => {
    const addonReachable = PLATFORM_PACKAGES.filter((p) => p.kind === "addon" && p.healthService).some((p) => {
      const h = findStatus(health, p.healthService!);
      return h?.reachable;
    });
    return addonReachable ? "full (inferred)" : "lite";
  }, [health]);

  const identity = findStatus(health, "identity");
  const coordinator = findStatus(health, "coordinator");
  const settings = findStatus(health, "settings");
  const gatewayHint = health.length > 0 ? "Aggregating via gateway ops" : "Awaiting health permission/data";

  return (
    <>
      <SectionHeader
        title="Platform Overview"
        description="Enterprise control plane for MicroServiceSystem — live probes where APIs exist."
        actions={
          <div className="btn-list">
            <Link className="btn" to="/services">
              Services
            </Link>
            <Link className="btn" to="/architecture">
              Architecture
            </Link>
          </div>
        }
      />

      {error ? <div className="alert alert-danger">{error}</div> : null}

      {loading ? (
        <div className="row row-cards mb-3">
          {[1, 2, 3, 4].map((i) => (
            <div className="col-sm-6 col-xl-3" key={i}>
              <Skeleton />
            </div>
          ))}
        </div>
      ) : (
        <div className="row row-cards mb-3">
          <div className="col-sm-6 col-xl-3">
            <MetricCard
              label="Healthy services"
              value={`${healthy}/${health.length || "—"}`}
              hint="From /ops health aggregate"
              tone="healthy"
            />
          </div>
          <div className="col-sm-6 col-xl-3">
            <MetricCard
              label="Attention"
              value={unhealthy}
              hint="Non-healthy or unreachable probes"
              tone={unhealthy > 0 ? "critical" : "healthy"}
            />
          </div>
          <div className="col-sm-6 col-xl-3">
            <MetricCard
              label="Dead letters"
              value={outbox?.deadLetterCount ?? "—"}
              hint={outbox ? `${outbox.service} outbox · ${outbox.pendingCount} pending` : "Outbox unavailable"}
              tone="messaging"
              action={
                <Link className="btn btn-sm" to="/messaging">
                  Open
                </Link>
              }
            />
          </div>
          <div className="col-sm-6 col-xl-3">
            <MetricCard
              label="Environment"
              value={profile}
              hint={`Admin ${ADMIN_VERSION} · framework .NET 10`}
              tone="info"
            />
          </div>
        </div>
      )}

      <div className="row row-cards mb-3">
        {[
          { id: "gateway", label: "Gateway", status: health.length ? "Healthy" : undefined, hint: gatewayHint },
          { id: "identity", label: "Identity", status: identity?.status, hint: identity?.description },
          { id: "coordinator", label: "Coordinator", status: coordinator?.status, hint: coordinator?.description },
          { id: "settings", label: "Settings", status: settings?.status, hint: settings?.description },
          { id: "postgres", label: "PostgreSQL", status: "Infra", hint: "Compose lite always-on" },
          { id: "redis", label: "Redis", status: "Infra", hint: "Compose lite always-on" },
          { id: "rabbitmq", label: "RabbitMQ", status: "Infra", hint: "Management :15672" },
          { id: "mongo", label: "MongoDB", status: "Optional", hint: "Profile full" },
        ].map((card) => (
          <div className="col-6 col-md-3" key={card.id}>
            <div className="card">
              <div className="card-body py-3">
                <div className="d-flex align-items-center gap-2">
                  <HealthIndicator status={card.status} />
                  <div className="fw-medium">{card.label}</div>
                  <div className="ms-auto">
                    <StatusBadge status={card.status} />
                  </div>
                </div>
                <div className="text-secondary small mt-1 text-truncate">{card.hint ?? "—"}</div>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="row row-cards">
        <div className="col-lg-6">
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Recent audit</h3>
              <div className="card-actions">
                <Link to="/audit">View all</Link>
              </div>
            </div>
            <div className="list-group list-group-flush">
              {audits.length === 0 ? (
                <div className="list-group-item text-secondary">No audit data (permission or full profile).</div>
              ) : (
                audits.map((item) => (
                  <div className="list-group-item" key={item.id}>
                    <div className="fw-medium">{item.action}</div>
                    <div className="text-secondary small">
                      {item.resourceType}/{item.resourceId}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
        <div className="col-lg-6">
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Recent logs</h3>
              <div className="card-actions">
                <Link to="/logs">View all</Link>
              </div>
            </div>
            <div className="list-group list-group-flush">
              {logs.length === 0 ? (
                <div className="list-group-item text-secondary">No logs (permission or full profile).</div>
              ) : (
                logs.map((item) => (
                  <div className="list-group-item" key={item.id}>
                    <div className="d-flex gap-2">
                      <StatusBadge status={item.level} tone="info" />
                      <div className="text-truncate">{item.message}</div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
        <div className="col-12">
          <PreviewBanner>
            Running sagas, deployments, and container CPU/memory tiles remain UI-only until ops APIs exist.
          </PreviewBanner>
          <div className="row row-cards">
            <div className="col-md-4">
              <MetricCard label="Running sagas" value="—" hint="Preview" tone="info" />
            </div>
            <div className="col-md-4">
              <MetricCard label="Latest deployments" value="—" hint="Preview" tone="infra" />
            </div>
            <div className="col-md-4">
              <MetricCard label="Package catalog" value={PLATFORM_PACKAGES.length} hint="Static inventory" tone="infra" />
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
