import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ApiClientError } from "@/api/client";
import { getInboxSummary, type OutboxService } from "@/api/ops";
import type { InboxSummary, OutboxDeadLetter } from "@/api/types";
import {
  DataTableShell,
  DetailDrawer,
  EventCard,
  MetricCard,
  PreviewBanner,
  QueueCard,
  Skeleton,
  StatusBadge,
  StepFlow,
  TableSkeleton,
  Timeline,
  type TimelineItem,
} from "@/components/control";
import { ErrorAlert } from "@/components/ui";
import { ExternalToolLink } from "@/platform/tools";
import {
  PREVIEW_BINDINGS,
  PREVIEW_CONSUMERS,
  PREVIEW_EVENT_FLOW,
  PREVIEW_EXCHANGES,
  PREVIEW_INSPECT_MESSAGE,
  PREVIEW_PUBLISHERS,
  PREVIEW_QUEUES,
  PREVIEW_RETRIES,
} from "./catalog";
import { useOutboxData } from "./useOutboxData";
import { OutboxServiceSelect, parseOutboxService } from "./OutboxServiceSelect";

function useOutboxServiceParam(): [OutboxService, (s: OutboxService) => void] {
  const [params, setParams] = useSearchParams();
  const service = parseOutboxService(params.get("service"));
  function setService(next: OutboxService) {
    const copy = new URLSearchParams(params);
    if (next === "identity") copy.delete("service");
    else copy.set("service", next);
    setParams(copy, { replace: true });
  }
  return [service, setService];
}

export function MessagingOverviewPage() {
  const [service, setService] = useOutboxServiceParam();
  const { summary, deadLetters, error, loading, load } = useOutboxData(service);

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
        <OutboxServiceSelect value={service} onChange={setService} />
        <button type="button" className="btn btn-sm" onClick={() => void load()} disabled={loading}>
          Refresh outbox
        </button>
      </div>
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
            <MetricCard label="Pending" value={summary?.pendingCount ?? "—"} tone="info" hint="Identity outbox" />
          </div>
          <div className="col-sm-6 col-lg-3">
            <MetricCard
              label="Dead letters"
              value={summary?.deadLetterCount ?? deadLetters.length}
              tone={(summary?.deadLetterCount ?? 0) > 0 ? "critical" : "healthy"}
              action={
                <Link className="btn btn-sm" to="/messaging/dead-letters">
                  Open
                </Link>
              }
            />
          </div>
          <div className="col-sm-6 col-lg-3">
            <MetricCard
              label="Deep links"
              value={
                <div className="btn-list flex-wrap">
                  <ExternalToolLink id="rabbitmq" />
                  <ExternalToolLink id="seq" />
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
        Outbox KPIs are live per service (gateway ops). Queue depth / broker topology remain preview.
      </PreviewBanner>
      <div className="row row-cards mb-3">
        <div className="col-md-4">
          <QueueCard
            title="Queues"
            count={PREVIEW_QUEUES.length}
            to="/messaging/queues"
            hint="Inventory"
            preview
          />
        </div>
        <div className="col-md-4">
          <QueueCard
            title="Exchanges"
            count={PREVIEW_EXCHANGES.length}
            to="/messaging/exchanges"
            hint="Inventory"
            preview
          />
        </div>
        <div className="col-md-4">
          <QueueCard
            title="Bindings"
            count={PREVIEW_BINDINGS.length}
            to="/messaging/bindings"
            hint="Inventory"
            preview
          />
        </div>
      </div>
      {!loading && deadLetters.length > 0 ? (
        <div className="mb-2">
          <div className="subheader mb-2">Recent dead letters (live)</div>
          <div className="row g-2">
            {deadLetters.slice(0, 3).map((item) => (
              <div className="col-md-4" key={item.id}>
                <EventCard
                  eventName={item.eventName}
                  service="Identity DLQ"
                  meta={`${item.attemptCount} attempts · ${item.error ?? "no error text"}`}
                  tone="critical"
                  actions={
                    <Link className="btn btn-sm" to={`/messaging/dead-letters?service=${service}&focus=${item.id}`}>
                      Inspect
                    </Link>
                  }
                />
              </div>
            ))}
          </div>
        </div>
      ) : null}
    </>
  );
}

function TopologyTable({
  title,
  rows,
  showDepth,
}: {
  title: string;
  rows: {
    name: string;
    type: string;
    consumers: number | string;
    ready?: number;
    unacked?: number;
    note?: string;
  }[];
  showDepth?: boolean;
}) {
  return (
    <>
      <PreviewBanner>Rabbit topology is illustrative until a management proxy or inventory API exists.</PreviewBanner>
      {showDepth ? (
        <div className="row row-cards mb-3">
          {rows.slice(0, 4).map((row) => (
            <div className="col-md-3" key={row.name}>
              <QueueCard
                title={row.name}
                hint={row.type}
                ready={row.ready ?? "—"}
                unacked={row.unacked ?? "—"}
                consumers={row.consumers}
                preview
              />
            </div>
          ))}
        </div>
      ) : null}
      <DataTableShell title={title}>
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Consumers</th>
              {showDepth ? (
                <>
                  <th>Ready</th>
                  <th>Unacked</th>
                </>
              ) : null}
              <th>Note</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.name}>
                <td>
                  <code>{row.name}</code>
                </td>
                <td>
                  <StatusBadge tone="messaging">{row.type}</StatusBadge>
                </td>
                <td className="text-secondary">{row.consumers}</td>
                {showDepth ? (
                  <>
                    <td className="text-secondary">{row.ready ?? "—"}</td>
                    <td className="text-secondary">{row.unacked ?? "—"}</td>
                  </>
                ) : null}
                <td className="text-secondary">{row.note ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
    </>
  );
}

export function MessagingQueuesPage() {
  return <TopologyTable title="Queues" rows={PREVIEW_QUEUES} showDepth />;
}

export function MessagingExchangesPage() {
  return <TopologyTable title="Exchanges" rows={PREVIEW_EXCHANGES} />;
}

export function MessagingBindingsPage() {
  return <TopologyTable title="Bindings" rows={PREVIEW_BINDINGS} />;
}

export function MessagingPublishersPage() {
  return (
    <>
      <PreviewBanner>Publisher registry is UI preview — derived from known integration events.</PreviewBanner>
      <div className="row g-2 mb-3">
        {PREVIEW_PUBLISHERS.map((row) => (
          <div className="col-md-4" key={row.id}>
            <EventCard
              eventName={row.event}
              service={row.service}
              meta={`exchange ${row.exchange}`}
            />
          </div>
        ))}
      </div>
      <DataTableShell title="Publishers">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Service</th>
              <th>Exchange</th>
              <th>Event</th>
            </tr>
          </thead>
          <tbody>
            {PREVIEW_PUBLISHERS.map((row) => (
              <tr key={row.id}>
                <td>{row.service}</td>
                <td>
                  <code>{row.exchange}</code>
                </td>
                <td>
                  <code>{row.event}</code>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
    </>
  );
}

export function MessagingConsumersPage() {
  return (
    <>
      <PreviewBanner>Consumer lag requires inbox/metrics APIs. Values below are placeholders.</PreviewBanner>
      <DataTableShell title="Consumers">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Service</th>
              <th>Queue</th>
              <th>Lag</th>
            </tr>
          </thead>
          <tbody>
            {PREVIEW_CONSUMERS.map((row) => (
              <tr key={row.id}>
                <td>{row.service}</td>
                <td>
                  <code>{row.queue}</code>
                </td>
                <td className="text-secondary">{row.lag}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
    </>
  );
}

function DeadLetterDrawer({
  item,
  onClose,
  canWrite,
  onRequeue,
}: {
  item: OutboxDeadLetter | null;
  onClose: () => void;
  canWrite: boolean;
  onRequeue: (id: string) => Promise<void>;
}) {
  const timeline: TimelineItem[] = useMemo(() => {
    if (!item) return [];
    return [
      {
        id: "occ",
        at: new Date(item.occurredOnUtc).toLocaleString(),
        title: "Occurred",
        detail: item.eventName,
        tone: "info",
      },
      {
        id: "attempts",
        at: "—",
        title: `${item.attemptCount} publish attempt(s)`,
        detail: item.error ?? "No error payload",
        tone: "degraded",
      },
      {
        id: "dlq",
        at: item.deadLetteredOnUtc ? new Date(item.deadLetteredOnUtc).toLocaleString() : "—",
        title: "Dead-lettered",
        detail: item.correlationId ? `correlation ${item.correlationId}` : "No correlation id",
        tone: "critical",
      },
    ];
  }, [item]);

  return (
    <DetailDrawer
      open={Boolean(item)}
      title={item ? item.eventName : "Dead letter"}
      onClose={onClose}
      footer={
        item && canWrite ? (
          <button type="button" className="btn btn-primary" onClick={() => void onRequeue(item.id)}>
            Requeue
          </button>
        ) : (
          <span className="text-secondary small">Requeue requires ops.outbox.write</span>
        )
      }
    >
      {item ? (
        <>
          <div className="datagrid mb-3">
            <div className="datagrid-item">
              <div className="datagrid-title">Id</div>
              <div className="datagrid-content msf-mono">{item.id}</div>
            </div>
            <div className="datagrid-item">
              <div className="datagrid-title">Tenant</div>
              <div className="datagrid-content msf-mono">{item.tenantId ?? "—"}</div>
            </div>
            <div className="datagrid-item">
              <div className="datagrid-title">Correlation</div>
              <div className="datagrid-content msf-mono">{item.correlationId ?? "—"}</div>
            </div>
          </div>
          <div className="subheader mb-2">Timeline</div>
          <Timeline items={timeline} />
          {item.correlationId ? (
            <div className="mt-3">
              <Link
                className="btn btn-sm"
                to={`/observability/correlation?q=${encodeURIComponent(item.correlationId)}`}
              >
                Open correlation
              </Link>
            </div>
          ) : null}
        </>
      ) : null}
    </DetailDrawer>
  );
}

export function MessagingDeadLettersPage() {
  const [service, setService] = useOutboxServiceParam();
  const { deadLetters, error, loading, canWrite, load, onRequeue } = useOutboxData(service);
  const [params, setParams] = useSearchParams();
  const focusId = params.get("focus");
  const selected = deadLetters.find((d) => d.id === focusId) ?? null;

  function select(id: string | null) {
    const next = new URLSearchParams(params);
    if (id) next.set("focus", id);
    else next.delete("focus");
    setParams(next, { replace: true });
  }

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
        <div className="d-flex align-items-center gap-2 flex-wrap">
          <OutboxServiceSelect value={service} onChange={setService} />
          <p className="text-secondary mb-0">Live outbox dead letters — inspect + requeue when permitted.</p>
        </div>
        <button type="button" className="btn btn-sm" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>
      <ErrorAlert error={error} />
      <DataTableShell title="Dead-lettered messages">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Event</th>
              <th>Attempts</th>
              <th>Dead-lettered</th>
              <th>Error</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {loading ? <TableSkeleton rows={5} cols={5} /> : null}
            {!loading && deadLetters.length === 0 ? (
              <tr>
                <td colSpan={5} className="text-secondary">
                  No dead letters.
                </td>
              </tr>
            ) : null}
            {deadLetters.map((item) => (
              <tr key={item.id} className={item.id === focusId ? "table-active" : undefined}>
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
                <td>
                  <div className="btn-list">
                    <button type="button" className="btn btn-sm" onClick={() => select(item.id)}>
                      Inspect
                    </button>
                    {canWrite ? (
                      <button type="button" className="btn btn-sm" onClick={() => void onRequeue(item.id)}>
                        Requeue
                      </button>
                    ) : null}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
      <DeadLetterDrawer
        item={selected}
        onClose={() => select(null)}
        canWrite={canWrite}
        onRequeue={async (id) => {
          await onRequeue(id);
          select(null);
        }}
      />
    </>
  );
}

export function MessagingOutboxPage() {
  const [service, setService] = useOutboxServiceParam();
  const { summary, deadLetters, pending, error, loading, load } = useOutboxData(service);

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
        <OutboxServiceSelect value={service} onChange={setService} />
        <button type="button" className="btn btn-sm" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>
      <ErrorAlert error={error} />
      <div className="row row-cards mb-3">
        <div className="col-md-4">
          <MetricCard label="Service" value={loading ? "…" : (summary?.service ?? "—")} tone="messaging" />
        </div>
        <div className="col-md-4">
          <MetricCard label="Pending" value={loading ? "…" : (summary?.pendingCount ?? "—")} tone="info" />
        </div>
        <div className="col-md-4">
          <MetricCard
            label="Dead letters"
            value={loading ? "…" : (summary?.deadLetterCount ?? "—")}
            tone={(summary?.deadLetterCount ?? 0) > 0 ? "critical" : "healthy"}
            action={
              <Link className="btn btn-sm" to={`/messaging/dead-letters?service=${service}`}>
                DLQ
              </Link>
            }
          />
        </div>
      </div>

      <DataTableShell title="Pending (metadata only — no payload)">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Event</th>
              <th>Occurred</th>
              <th>Attempts</th>
              <th>Correlation</th>
              <th>Locked until</th>
            </tr>
          </thead>
          <tbody>
            {loading ? <TableSkeleton rows={4} cols={5} /> : null}
            {!loading && pending.length === 0 ? (
              <tr>
                <td colSpan={5} className="text-secondary">
                  No pending outbox rows.
                </td>
              </tr>
            ) : null}
            {pending.map((row) => (
              <tr key={row.id}>
                <td>
                  <code>{row.eventName}</code>
                </td>
                <td className="text-secondary">{new Date(row.occurredOnUtc).toLocaleString()}</td>
                <td>{row.attemptCount}</td>
                <td className="text-secondary msf-mono small">{row.correlationId ?? "—"}</td>
                <td className="text-secondary">
                  {row.lockedUntilUtc ? new Date(row.lockedUntilUtc).toLocaleString() : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>

      {deadLetters.length > 0 ? (
        <div className="row g-2 mt-3">
          {deadLetters.slice(0, 4).map((item) => (
            <div className="col-md-6" key={item.id}>
              <EventCard
                eventName={item.eventName}
                service={service}
                meta={`DLQ · ${item.attemptCount} attempts`}
                tone="critical"
                actions={
                  <Link className="btn btn-sm" to={`/messaging/dead-letters?service=${service}&focus=${item.id}`}>
                    Open
                  </Link>
                }
              />
            </div>
          ))}
        </div>
      ) : null}
    </>
  );
}

export function MessagingInboxPage() {
  const [service, setService] = useOutboxServiceParam();
  const [summary, setSummary] = useState<InboxSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setSummary(await getInboxSummary(service));
    } catch (err) {
      setSummary(null);
      setError(err instanceof ApiClientError ? err.message : "Failed to load inbox summary.");
    } finally {
      setLoading(false);
    }
  }, [service]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
        <OutboxServiceSelect value={service} onChange={setService} />
        <button type="button" className="btn btn-sm" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>
      <ErrorAlert error={error} />
      <div className="row row-cards mb-3">
        <div className="col-md-3">
          <MetricCard label="Processed" value={loading ? "…" : (summary?.processedCount ?? "—")} tone="healthy" />
        </div>
        <div className="col-md-3">
          <MetricCard label="Open" value={loading ? "…" : (summary?.openCount ?? "—")} tone="info" />
        </div>
        <div className="col-md-3">
          <MetricCard label="In flight" value={loading ? "…" : (summary?.inFlightCount ?? "—")} tone="messaging" />
        </div>
        <div className="col-md-3">
          <MetricCard
            label="Failed"
            value={loading ? "…" : (summary?.failedCount ?? "—")}
            tone={(summary?.failedCount ?? 0) > 0 ? "critical" : "healthy"}
          />
        </div>
      </div>
      <p className="text-secondary small mb-0">
        Counts only — no inbox key dump (Architect C4). Requires <code>ops.inbox.read</code>.
      </p>
    </>
  );
}

export function MessagingEventFlowPage() {
  return (
    <>
      <PreviewBanner>
        Choreography illustration aligned with docs/distributed-workflows.md — not a live topology feed.
      </PreviewBanner>
      <div className="card mb-3">
        <div className="card-body">
          <StepFlow
            steps={PREVIEW_EVENT_FLOW.map((hop, i) => ({
              id: hop.id,
              label: hop.label,
              detail: hop.detail,
              status: i < PREVIEW_EVENT_FLOW.length - 1 ? "done" : "done",
            }))}
          />
        </div>
      </div>
      <div className="btn-list">
        <Link className="btn" to="/workflows">
          Workflow Center
        </Link>
        <Link className="btn" to="/architecture/event-flow">
          Architecture event flow
        </Link>
      </div>
    </>
  );
}

export function MessagingRetriesPage() {
  return (
    <>
      <PreviewBanner>
        Retry schedules are framework-side. Identity DLQ requeue is the only live mutation today.
      </PreviewBanner>
      <DataTableShell title="Scheduled retries (preview)">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Event</th>
              <th>Attempt</th>
              <th>Next</th>
              <th>Reason</th>
            </tr>
          </thead>
          <tbody>
            {PREVIEW_RETRIES.map((row) => (
              <tr key={row.id}>
                <td>
                  <code>{row.eventName}</code>
                </td>
                <td>{row.attempt}</td>
                <td className="text-secondary">{row.nextAt}</td>
                <td className="text-secondary">{row.reason}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
      <div className="mt-3 btn-list">
        <Link className="btn" to="/messaging/dead-letters">
          Identity DLQ
        </Link>
        <Link className="btn" to="/messaging/replay">
          Replay
        </Link>
      </div>
    </>
  );
}

export function MessagingReplayPage() {
  return (
    <>
      <PreviewBanner>
        Broader replay tooling stays disabled until an ops contract exists. Use DLQ requeue for Identity.
      </PreviewBanner>
      <div className="card">
        <div className="card-body">
          <h3 className="card-title">Replay selected</h3>
          <p className="text-secondary">
            Select messages from Inspect / DLQ once a multi-service replay API is approved. No silent fake
            success.
          </p>
          <button type="button" className="btn btn-primary" disabled title="No ops API">
            Replay selected
          </button>
          <Link className="btn ms-2" to="/messaging/dead-letters">
            Open DLQ
          </Link>
        </div>
      </div>
    </>
  );
}

export function MessagingInspectPage() {
  const msg = PREVIEW_INSPECT_MESSAGE;
  const timeline: TimelineItem[] = [
    { id: "t1", at: "—", title: "Published to outbox", detail: "Same UoW as domain write", tone: "info" },
    { id: "t2", at: "—", title: "Relay attempted", detail: msg.routingKey, tone: "messaging" },
    { id: "t3", at: "—", title: "Inspect sample", detail: "Replace when message-get API exists", tone: "degraded" },
  ];

  return (
    <>
      <PreviewBanner>Message inspect uses sample payload — live DLQ inspect is on Dead letters.</PreviewBanner>
      <div className="row">
        <div className="col-lg-7">
          <div className="card mb-3">
            <div className="card-header">
              <h3 className="card-title">Inspect message</h3>
              <div className="card-actions">
                <StatusBadge tone="messaging">preview</StatusBadge>
              </div>
            </div>
            <div className="card-body">
              <div className="datagrid mb-3">
                <div className="datagrid-item">
                  <div className="datagrid-title">Id</div>
                  <div className="datagrid-content msf-mono">{msg.id}</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">Event</div>
                  <div className="datagrid-content">
                    <code>{msg.eventName}</code>
                  </div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">Routing key</div>
                  <div className="datagrid-content msf-mono">{msg.routingKey}</div>
                </div>
                <div className="datagrid-item">
                  <div className="datagrid-title">Correlation</div>
                  <div className="datagrid-content msf-mono">{msg.correlationId}</div>
                </div>
              </div>
              <div className="subheader">Payload</div>
              <pre className="bg-dark text-light p-3 rounded" style={{ whiteSpace: "pre-wrap" }}>
                {msg.payload}
              </pre>
            </div>
          </div>
        </div>
        <div className="col-lg-5">
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Timeline</h3>
            </div>
            <div className="card-body">
              <Timeline items={timeline} />
            </div>
          </div>
          <div className="mt-3">
            <Link className="btn" to="/messaging/dead-letters">
              Inspect live DLQ
            </Link>
          </div>
        </div>
      </div>
    </>
  );
}

export function MessagingTimelinePage() {
  const [service, setService] = useOutboxServiceParam();
  const { deadLetters, loading, error, load } = useOutboxData(service);
  const items: TimelineItem[] = deadLetters.slice(0, 8).map((item) => ({
    id: item.id,
    at: item.deadLetteredOnUtc ? new Date(item.deadLetteredOnUtc).toLocaleString() : "—",
    title: item.eventName,
    detail: item.error ?? `${item.attemptCount} attempts`,
    tone: "critical",
  }));

  return (
    <>
      <div className="d-flex justify-content-between mb-3 gap-2 flex-wrap">
        <OutboxServiceSelect value={service} onChange={setService} />
        <button type="button" className="btn btn-sm" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>
      <PreviewBanner>
        Timeline shows live DLQ events for the selected service. Broader broker timelines await API.
      </PreviewBanner>
      <ErrorAlert error={error} />
      <div className="card">
        <div className="card-body">
          {loading ? <Skeleton height={120} /> : <Timeline items={items} empty="No DLQ timeline events." />}
        </div>
      </div>
    </>
  );
}
