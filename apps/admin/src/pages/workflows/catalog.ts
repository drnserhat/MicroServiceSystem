import type { HealthTone } from "@/components/control/tones";
import type { StepFlowStep, TimelineItem } from "@/components/control";

export type SagaBoard =
  | "Running"
  | "Completed"
  | "Failed"
  | "Compensated"
  | "Waiting"
  | "Retrying";

export type SagaPreview = {
  id: string;
  name: string;
  state: SagaBoard;
  duration: string;
  startedAt: string;
  correlationId: string;
  currentStep: string;
};

export const BOARD_META: {
  id: SagaBoard;
  path: string;
  tone: HealthTone;
  hint: string;
}[] = [
  { id: "Running", path: "running", tone: "info", hint: "In-flight orchestrations" },
  { id: "Completed", path: "completed", tone: "healthy", hint: "Happy path finished" },
  { id: "Failed", path: "failed", tone: "critical", hint: "Terminal step failure" },
  { id: "Compensated", path: "compensated", tone: "degraded", hint: "Undo path applied" },
  { id: "Waiting", path: "waiting", tone: "messaging", hint: "Lease / remote await" },
  { id: "Retrying", path: "retrying", tone: "degraded", hint: "Transient retry" },
];

/** Reference RegisterUser happy-path + compensation education steps */
export const REGISTER_USER_STEPS: StepFlowStep[] = [
  { id: "identity", label: "Identity", detail: "RegisterIdentityStep", status: "done" },
  { id: "user", label: "User", detail: "CreateUserProfileStep", status: "done" },
  { id: "notification", label: "Notification", detail: "Welcome via outbox", status: "done" },
  { id: "audit", label: "Audit", detail: "Audit event via outbox", status: "done" },
  { id: "completed", label: "Completed", detail: "Saga checkpoint final", status: "done" },
];

export const REGISTER_USER_COMPENSATION: StepFlowStep[] = [
  { id: "identity", label: "Identity", detail: "Registered", status: "done" },
  { id: "user", label: "User", detail: "Profile failed", status: "failed" },
  {
    id: "compensate",
    label: "Identity.Disable",
    detail: "Compensation (internal API key)",
    status: "compensated",
  },
];

export const PREVIEW_SAGAS: SagaPreview[] = [
  {
    id: "saga-running-01",
    name: "RegisterUser",
    state: "Running",
    duration: "0.4s",
    startedAt: "2026-07-30T10:12:01Z",
    correlationId: "corr-ru-1001",
    currentStep: "CreateUserProfileStep",
  },
  {
    id: "saga-waiting-01",
    name: "RegisterUser",
    state: "Waiting",
    duration: "2.1s",
    startedAt: "2026-07-30T10:11:40Z",
    correlationId: "corr-ru-1000",
    currentStep: "Lease renewed — awaiting User",
  },
  {
    id: "saga-retry-01",
    name: "RegisterUser",
    state: "Retrying",
    duration: "1.8s",
    startedAt: "2026-07-30T10:10:55Z",
    correlationId: "corr-ru-0998",
    currentStep: "RegisterIdentityStep (attempt 2)",
  },
  {
    id: "saga-done-01",
    name: "RegisterUser",
    state: "Completed",
    duration: "1.2s",
    startedAt: "2026-07-30T09:58:12Z",
    correlationId: "corr-ru-0990",
    currentStep: "Completed",
  },
  {
    id: "saga-done-02",
    name: "RegisterUser",
    state: "Completed",
    duration: "0.9s",
    startedAt: "2026-07-30T09:40:03Z",
    correlationId: "corr-ru-0985",
    currentStep: "Completed",
  },
  {
    id: "saga-failed-01",
    name: "RegisterUser",
    state: "Failed",
    duration: "0.7s",
    startedAt: "2026-07-30T09:22:44Z",
    correlationId: "corr-ru-0970",
    currentStep: "CreateUserProfileStep",
  },
  {
    id: "saga-comp-01",
    name: "RegisterUser",
    state: "Compensated",
    duration: "1.5s",
    startedAt: "2026-07-30T08:55:18Z",
    correlationId: "corr-ru-0960",
    currentStep: "Identity.Disable",
  },
];

export function sagasForBoard(board: SagaBoard): SagaPreview[] {
  return PREVIEW_SAGAS.filter((s) => s.state === board);
}

export function findSaga(id: string): SagaPreview | undefined {
  return PREVIEW_SAGAS.find((s) => s.id === id);
}

export function stepsForSaga(saga: SagaPreview): StepFlowStep[] {
  if (saga.state === "Compensated") return REGISTER_USER_COMPENSATION;
  if (saga.state === "Failed") {
    return [
      { id: "identity", label: "Identity", detail: "Done", status: "done" },
      { id: "user", label: "User", detail: "Failed", status: "failed" },
      { id: "notification", label: "Notification", detail: "Skipped", status: "pending" },
      { id: "audit", label: "Audit", detail: "Skipped", status: "pending" },
      { id: "completed", label: "Completed", detail: "Not reached", status: "pending" },
    ];
  }
  if (saga.state === "Completed") return REGISTER_USER_STEPS;
  if (saga.state === "Running" || saga.state === "Waiting" || saga.state === "Retrying") {
    return [
      { id: "identity", label: "Identity", detail: "Done", status: "done" },
      {
        id: "user",
        label: "User",
        detail: saga.currentStep,
        status: saga.state === "Retrying" ? "active" : "active",
      },
      { id: "notification", label: "Notification", detail: "Pending", status: "pending" },
      { id: "audit", label: "Audit", detail: "Pending", status: "pending" },
      { id: "completed", label: "Completed", detail: "Pending", status: "pending" },
    ];
  }
  return REGISTER_USER_STEPS;
}

export function timelineForSaga(saga: SagaPreview): TimelineItem[] {
  const base: TimelineItem[] = [
    {
      id: `${saga.id}-t0`,
      at: saga.startedAt,
      title: "Saga started",
      detail: `${saga.name} · correlation ${saga.correlationId}`,
      tone: "info",
    },
    {
      id: `${saga.id}-t1`,
      at: saga.startedAt,
      title: "Checkpoint: Identity registered",
      detail: "ISagaCheckpoint flush + lease renew",
      tone: "healthy",
    },
  ];

  if (saga.state === "Running" || saga.state === "Waiting" || saga.state === "Retrying") {
    base.push({
      id: `${saga.id}-t2`,
      at: "—",
      title: saga.state === "Retrying" ? "Retrying current step" : "Awaiting User profile",
      detail: saga.currentStep,
      tone: saga.state === "Retrying" ? "degraded" : "messaging",
    });
    return base;
  }

  if (saga.state === "Completed") {
    base.push(
      {
        id: `${saga.id}-t2`,
        at: "—",
        title: "User profile created",
        detail: "CreateUserProfileStep",
        tone: "healthy",
      },
      {
        id: `${saga.id}-t3`,
        at: "—",
        title: "Welcome + Audit published",
        detail: "Transactional outbox (choreography fan-out)",
        tone: "messaging",
      },
      {
        id: `${saga.id}-t4`,
        at: "—",
        title: "Saga completed",
        detail: `Duration ${saga.duration}`,
        tone: "healthy",
      },
    );
    return base;
  }

  if (saga.state === "Failed") {
    base.push({
      id: `${saga.id}-t2`,
      at: "—",
      title: "User profile step failed",
      detail: "Terminal failure — inspect Seq / Coordinator logs",
      tone: "critical",
    });
    return base;
  }

  base.push(
    {
      id: `${saga.id}-t2`,
      at: "—",
      title: "User profile step failed",
      detail: "Triggering compensation",
      tone: "critical",
    },
    {
      id: `${saga.id}-t3`,
      at: "—",
      title: "Compensation: Identity.Disable",
      detail: "Internal API key — asymmetric undo",
      tone: "degraded",
    },
    {
      id: `${saga.id}-t4`,
      at: "—",
      title: "Saga compensated",
      detail: `Duration ${saga.duration}`,
      tone: "degraded",
    },
  );
  return base;
}

export function boardTone(state: SagaBoard): HealthTone {
  return BOARD_META.find((b) => b.id === state)?.tone ?? "unknown";
}
