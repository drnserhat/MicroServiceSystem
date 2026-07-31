import { Link, useParams } from "react-router-dom";
import type { SagaItem } from "@/api/types";
import {
  DataTableShell,
  MetricCard,
  PreviewBanner,
  Skeleton,
  StatusBadge,
  StepFlow,
  Timeline,
  WorkflowCard,
  type TimelineItem,
} from "@/components/control";
import { ErrorAlert } from "@/components/ui";
import {
  BOARD_META,
  REGISTER_USER_COMPENSATION,
  REGISTER_USER_STEPS,
  boardTone,
  type SagaBoard,
} from "./catalog";
import { durationLabel, mapSagaToBoard, useSagaDetail, useSagaList } from "./useSagas";

export function WorkflowsOverviewPage() {
  const { byBoard, items, loading, error, load } = useSagaList();
  const counts = Object.fromEntries(BOARD_META.map((b) => [b.id, byBoard[b.id].length])) as Record<
    SagaBoard,
    number
  >;

  return (
    <>
      <div className="d-flex justify-content-end mb-3">
        <button type="button" className="btn btn-sm" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>
      <ErrorAlert error={error} />
      <PreviewBanner>
        Live RegisterUser sagas from Coordinator ops. Boards map domain states (Compensating → Compensated board).
        Definitions remain educational.
      </PreviewBanner>

      <div className="row row-cards mb-3">
        <div className="col-sm-6 col-lg-3">
          <MetricCard label="Definition" value="RegisterUser" tone="info" hint="Reference saga" />
        </div>
        <div className="col-sm-6 col-lg-3">
          <MetricCard
            label="Running"
            value={loading ? "…" : counts.Running + counts.Waiting + counts.Retrying}
            tone="info"
          />
        </div>
        <div className="col-sm-6 col-lg-3">
          <MetricCard
            label="Failed"
            value={loading ? "…" : counts.Failed}
            tone={counts.Failed > 0 ? "critical" : "healthy"}
          />
        </div>
        <div className="col-sm-6 col-lg-3">
          <MetricCard
            label="Compensating"
            value={loading ? "…" : counts.Compensated}
            tone={counts.Compensated > 0 ? "degraded" : "healthy"}
          />
        </div>
      </div>

      <div className="card mb-3">
        <div className="card-header">
          <h3 className="card-title">RegisterUser happy path</h3>
          <div className="card-actions">
            <Link className="btn btn-sm" to="/workflows/definitions">
              Full definition
            </Link>
          </div>
        </div>
        <div className="card-body">
          <StepFlow steps={REGISTER_USER_STEPS} compact />
        </div>
      </div>

      <div className="row row-cards mb-3">
        {BOARD_META.map((board) => (
          <div className="col-6 col-md-4 col-xl-2" key={board.id}>
            <WorkflowCard
              title={board.id}
              count={loading ? "…" : counts[board.id]}
              hint={board.hint}
              to={`/workflows/${board.path}`}
              tone={board.tone}
            />
          </div>
        ))}
      </div>

      {loading ? <Skeleton height={160} /> : <SagaTable title="Recent instances" sagas={items.slice(0, 8)} />}
    </>
  );
}

export function WorkflowsBoardsPage() {
  const { byBoard, loading, error, load } = useSagaList();

  return (
    <>
      <div className="d-flex justify-content-end mb-3">
        <button type="button" className="btn btn-sm" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>
      <ErrorAlert error={error} />
      <div className="row row-cards">
        {BOARD_META.map((board) => {
          const boardItems = byBoard[board.id];
          return (
            <div className="col-md-6 col-xl-4" key={board.id}>
              <div className="card h-100">
                <div className="card-header">
                  <h3 className="card-title">
                    <StatusBadge tone={board.tone}>{board.id}</StatusBadge>
                    <span className="ms-2 text-secondary small">{boardItems.length}</span>
                  </h3>
                </div>
                <div className="list-group list-group-flush">
                  {loading ? (
                    <div className="list-group-item text-secondary">Loading…</div>
                  ) : boardItems.length === 0 ? (
                    <div className="list-group-item text-secondary">Empty board</div>
                  ) : (
                    boardItems.map((saga) => (
                      <Link
                        key={saga.id}
                        className="list-group-item list-group-item-action"
                        to={`/workflows/${saga.id}`}
                      >
                        <div className="fw-medium">
                          <code>{saga.name}</code>
                        </div>
                        <div className="text-secondary small text-truncate">
                          {saga.currentStep} · {durationLabel(saga)}
                        </div>
                      </Link>
                    ))
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </>
  );
}

export function WorkflowsDefinitionsPage() {
  return (
    <>
      <PreviewBanner>
        Education surface for Coordinator RegisterUserSaga — lease recovery and compensation. Live instances are
        on Overview / Boards.
      </PreviewBanner>
      <div className="card mb-3">
        <div className="card-header">
          <h3 className="card-title">Happy path</h3>
        </div>
        <div className="card-body">
          <StepFlow steps={REGISTER_USER_STEPS} />
        </div>
      </div>
      <div className="card mb-3">
        <div className="card-header">
          <h3 className="card-title">Compensation path</h3>
        </div>
        <div className="card-body">
          <StepFlow steps={REGISTER_USER_COMPENSATION} />
        </div>
      </div>
      <div className="btn-list">
        <Link className="btn btn-primary" to="/users/register">
          Try registration
        </Link>
        <Link className="btn" to="/messaging/event-flow">
          Event flow
        </Link>
      </div>
    </>
  );
}

function BoardListPage({ board, apiState }: { board: SagaBoard; apiState?: string }) {
  const { items, byBoard, loading, error, load } = useSagaList(apiState);
  const sagas = apiState ? items : byBoard[board];

  return (
    <>
      <div className="d-flex justify-content-end mb-3">
        <button type="button" className="btn btn-sm" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>
      <ErrorAlert error={error} />
      {loading ? <Skeleton height={120} /> : <SagaTable title={`${board} instances`} sagas={sagas} />}
    </>
  );
}

export function WorkflowsRunningPage() {
  return <BoardListPage board="Running" />;
}
export function WorkflowsCompletedPage() {
  return <BoardListPage board="Completed" apiState="Completed" />;
}
export function WorkflowsFailedPage() {
  return <BoardListPage board="Failed" apiState="Failed" />;
}
export function WorkflowsCompensatedPage() {
  return <BoardListPage board="Compensated" apiState="Compensating" />;
}
export function WorkflowsWaitingPage() {
  return <BoardListPage board="Waiting" />;
}
export function WorkflowsRetryingPage() {
  return <BoardListPage board="Retrying" />;
}

export function WorkflowsDetailPage() {
  const { sagaId } = useParams<{ sagaId: string }>();
  const { item, loading, error } = useSagaDetail(sagaId);

  if (loading) return <Skeleton height={240} />;
  if (error) return <ErrorAlert error={error} />;
  if (!item) {
    return (
      <div className="card">
        <div className="card-body">
          <h3 className="card-title">Saga not found</h3>
          <Link className="btn" to="/workflows">
            Back
          </Link>
        </div>
      </div>
    );
  }

  const board = mapSagaToBoard(item);
  const timeline: TimelineItem[] = [
    {
      id: "created",
      at: new Date(item.createdAtUtc).toLocaleString(),
      title: "Saga started",
      detail: `${item.email} · ${item.userName}`,
      tone: "info",
    },
    {
      id: "state",
      at: item.modifiedAtUtc ? new Date(item.modifiedAtUtc).toLocaleString() : "—",
      title: `State: ${item.state}`,
      detail: item.currentStep,
      tone: boardTone(board),
    },
  ];
  if (item.failureReason) {
    timeline.push({
      id: "fail",
      at: "—",
      title: "Failure / compensation reason",
      detail: item.failureReason,
      tone: "critical",
    });
  }

  const steps =
    item.state === "Compensating"
      ? REGISTER_USER_COMPENSATION
      : item.state === "Completed"
        ? REGISTER_USER_STEPS
        : [
            {
              id: "identity",
              label: "Identity",
              detail: item.identityUserId ? "Done" : "Pending",
              status: item.identityUserId ? ("done" as const) : ("pending" as const),
            },
            {
              id: "user",
              label: "User",
              detail: item.currentStep,
              status:
                item.state === "Failed"
                  ? ("failed" as const)
                  : item.userProfileId
                    ? ("done" as const)
                    : ("active" as const),
            },
            {
              id: "done",
              label: "Completed",
              detail: item.isTerminal ? item.state : "Pending",
              status: item.state === "Completed" ? ("done" as const) : ("pending" as const),
            },
          ];

  return (
    <>
      <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
        <h2 className="mb-0">
          <code>{item.name}</code>
        </h2>
        <StatusBadge tone={boardTone(board)}>{item.state}</StatusBadge>
        <span className="text-secondary small ms-auto">id {item.id}</span>
      </div>

      <div className="row row-cards mb-3">
        <div className="col-md-4">
          <MetricCard label="Duration" value={durationLabel(item)} />
        </div>
        <div className="col-md-4">
          <MetricCard label="Current step" value={item.currentStep} tone="info" />
        </div>
        <div className="col-md-4">
          <MetricCard label="Email" value={<span className="fs-6">{item.email}</span>} />
        </div>
      </div>

      <div className="card mb-3">
        <div className="card-header">
          <h3 className="card-title">Step flow</h3>
        </div>
        <div className="card-body">
          <StepFlow steps={steps} />
        </div>
      </div>

      <div className="card mb-3">
        <div className="card-header">
          <h3 className="card-title">Timeline</h3>
        </div>
        <div className="card-body">
          <Timeline items={timeline} />
        </div>
      </div>

      <div className="card">
        <div className="card-body text-secondary">
          <p className="mb-2">
            Lease: {item.lockedBy ?? "—"} until{" "}
            {item.lockedUntilUtc ? new Date(item.lockedUntilUtc).toLocaleString() : "—"}. Recovery only claims
            expired leases.
          </p>
          <div className="btn-list">
            <Link className="btn btn-sm" to="/messaging/outbox?service=coordinator">
              Coordinator outbox
            </Link>
            <Link className="btn btn-sm" to="/workflows/definitions">
              Definitions
            </Link>
          </div>
        </div>
      </div>
    </>
  );
}

function SagaTable({ title, sagas }: { title: string; sagas: SagaItem[] }) {
  return (
    <DataTableShell title={title}>
      <table className="table table-vcenter card-table">
        <thead>
          <tr>
            <th>Saga</th>
            <th>State</th>
            <th>Step</th>
            <th>Duration</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {sagas.length === 0 ? (
            <tr>
              <td colSpan={5} className="text-secondary">
                No instances.
              </td>
            </tr>
          ) : (
            sagas.map((saga) => {
              const board = mapSagaToBoard(saga);
              return (
                <tr key={saga.id}>
                  <td>
                    <code>{saga.name}</code>
                    <div className="text-secondary small">{saga.email}</div>
                  </td>
                  <td>
                    <StatusBadge tone={boardTone(board)}>{saga.state}</StatusBadge>
                  </td>
                  <td className="text-secondary">{saga.currentStep}</td>
                  <td className="text-secondary">{durationLabel(saga)}</td>
                  <td>
                    <Link className="btn btn-sm" to={`/workflows/${saga.id}`}>
                      Inspect
                    </Link>
                  </td>
                </tr>
              );
            })
          )}
        </tbody>
      </table>
    </DataTableShell>
  );
}
