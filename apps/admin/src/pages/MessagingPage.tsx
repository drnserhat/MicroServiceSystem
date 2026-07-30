import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiClientError } from "@/api/client";
import { getOutboxSnapshot, requeueDeadLetter } from "@/api/ops";
import type { OutboxDeadLetter, OutboxSummary } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import {
  MetricCard,
  PreviewBanner,
  SectionHeader,
  Skeleton,
  StatusBadge,
} from "@/components/control";
import { ErrorAlert } from "@/components/ui";

const PREVIEW_QUEUES = [
  { name: "identity.events", type: "queue", consumers: 1 },
  { name: "user.events", type: "queue", consumers: 1 },
  { name: "notification.events", type: "queue", consumers: 0 },
  { name: "msf.exchange", type: "exchange", consumers: "—" },
];

export function MessagingPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.OpsOutboxRead}>
      <MessagingInner />
    </RequirePermission>
  );
}

function MessagingInner() {
  const { can } = useAuth();
  const canWrite = can(FrameworkPermissions.OpsOutboxWrite);
  const [summary, setSummary] = useState<OutboxSummary | null>(null);
  const [deadLetters, setDeadLetters] = useState<OutboxDeadLetter[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await getOutboxSnapshot();
      setSummary(data.summary);
      setDeadLetters(data.deadLetters);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : "Failed to load outbox.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onRequeue(id: string) {
    if (!canWrite) return;
    try {
      await requeueDeadLetter(id);
      await load();
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : "Requeue failed.");
    }
  }

  return (
    <>
      <SectionHeader
        title="Messaging Center"
        description="Transactional outbox (Identity) plus RabbitMQ topology preview."
        actions={
          <div className="btn-list">
            <a className="btn" href="http://localhost:15672" target="_blank" rel="noreferrer">
              RabbitMQ UI
            </a>
            <button type="button" className="btn" onClick={() => void load()} disabled={loading}>
              Refresh
            </button>
          </div>
        }
      />
      <ErrorAlert error={error} />

      {loading ? (
        <div className="row row-cards mb-3">
          {[1, 2, 3, 4].map((i) => (
            <div className="col-sm-6 col-lg-3" key={i}>
              <Skeleton height={100} />
            </div>
          ))}
        </div>
      ) : (
        <div className="row row-cards mb-3">
          <div className="col-sm-6 col-lg-3">
            <MetricCard label="Service" value={summary?.service ?? "—"} tone="messaging" />
          </div>
          <div className="col-sm-6 col-lg-3">
            <MetricCard
              label="Pending"
              value={summary?.pendingCount ?? "—"}
              tone="info"
              hint="Identity outbox"
            />
          </div>
          <div className="col-sm-6 col-lg-3">
            <MetricCard
              label="Dead letters"
              value={summary?.deadLetterCount ?? "—"}
              tone={(summary?.deadLetterCount ?? 0) > 0 ? "critical" : "healthy"}
            />
          </div>
          <div className="col-sm-6 col-lg-3">
            <MetricCard
              label="Deep links"
              value={
                <div className="btn-list flex-wrap">
                  <a className="btn btn-sm" href="http://localhost:5341" target="_blank" rel="noreferrer">
                    Seq
                  </a>
                  <a className="btn btn-sm" href="http://localhost:16686" target="_blank" rel="noreferrer">
                    Jaeger
                  </a>
                  <Link className="btn btn-sm" to="/workflows">
                    Sagas
                  </Link>
                </div>
              }
            />
          </div>
        </div>
      )}

      <PreviewBanner>
        Queues, exchanges, bindings, and inbox panels below are illustrative. Only Identity outbox is live.
      </PreviewBanner>

      <div className="row row-cards mb-3">
        <div className="col-lg-6">
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Topology preview</h3>
            </div>
            <div className="table-responsive">
              <table className="table table-vcenter card-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Type</th>
                    <th>Consumers</th>
                  </tr>
                </thead>
                <tbody>
                  {PREVIEW_QUEUES.map((row) => (
                    <tr key={row.name}>
                      <td>
                        <code>{row.name}</code>
                      </td>
                      <td>
                        <StatusBadge tone="messaging">{row.type}</StatusBadge>
                      </td>
                      <td className="text-secondary">{row.consumers}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
        <div className="col-lg-6">
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Inbox / consumer lag</h3>
            </div>
            <div className="card-body text-secondary">
              Multi-service inbox metrics and consumer lag require a messaging inventory API. Use RabbitMQ
              management for live queue depth until then.
            </div>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Dead-lettered messages (Identity outbox)</h3>
        </div>
        <div className="table-responsive">
          <table className="table table-vcenter card-table">
            <thead>
              <tr>
                <th>Event</th>
                <th>Attempts</th>
                <th>Dead-lettered</th>
                <th>Error</th>
                {canWrite ? <th /> : null}
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className="text-secondary">
                    Loading…
                  </td>
                </tr>
              ) : null}
              {!loading && deadLetters.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-secondary">
                    No dead letters.
                  </td>
                </tr>
              ) : null}
              {deadLetters.map((item) => (
                <tr key={item.id}>
                  <td>
                    <code>{item.eventName}</code>
                  </td>
                  <td>{item.attemptCount}</td>
                  <td className="text-secondary">
                    {item.deadLetteredOnUtc ? new Date(item.deadLetteredOnUtc).toLocaleString() : "—"}
                  </td>
                  <td className="text-secondary text-truncate" style={{ maxWidth: 280 }}>
                    {item.error ?? "—"}
                  </td>
                  {canWrite ? (
                    <td>
                      <button type="button" className="btn btn-sm" onClick={() => void onRequeue(item.id)}>
                        Requeue
                      </button>
                    </td>
                  ) : null}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
