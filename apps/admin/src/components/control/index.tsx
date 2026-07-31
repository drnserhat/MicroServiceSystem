import type { HealthTone } from "./tones";
import { badgeClass, toneFromStatus } from "./tones";
import { useState } from "react";
import { Link } from "react-router-dom";
import { useToast } from "@/ui/toast/ToastContext";

export { HubTabs, type HubTab } from "./HubTabs";
export { FilterBar } from "./FilterBar";
export { DependencyGraph, MapNodeInspector, buildHealthLookup } from "./PlatformMap";
export { QueueCard, EventCard, DetailDrawer } from "./MessagingPrimitives";

const toneColor: Record<HealthTone, string> = {
  healthy: "var(--msf-healthy)",
  degraded: "var(--msf-degraded)",
  critical: "var(--msf-critical)",
  info: "var(--msf-info)",
  messaging: "var(--msf-messaging)",
  infra: "var(--msf-infra)",
  unknown: "var(--msf-infra)",
};

export function StatusBadge({
  status,
  reachable,
  tone,
  children,
}: {
  status?: string;
  reachable?: boolean;
  tone?: HealthTone;
  children?: React.ReactNode;
}) {
  const resolved = tone ?? toneFromStatus(status, reachable);
  return <span className={badgeClass(resolved)}>{children ?? status ?? "Unknown"}</span>;
}

export function HealthBadge(props: {
  status?: string;
  reachable?: boolean;
  tone?: HealthTone;
  children?: React.ReactNode;
}) {
  return <StatusBadge {...props} />;
}

export function HealthIndicator({
  status,
  reachable,
  size = 10,
}: {
  status?: string;
  reachable?: boolean;
  size?: number;
}) {
  const tone = toneFromStatus(status, reachable);
  const color = toneColor[tone];

  return (
    <span
      title={status ?? "unknown"}
      style={{
        display: "inline-block",
        width: size,
        height: size,
        borderRadius: "50%",
        background: color,
        boxShadow: `0 0 0 2px color-mix(in srgb, ${color} 25%, transparent)`,
      }}
    />
  );
}

export function MetricCard({
  label,
  value,
  hint,
  tone,
  action,
}: {
  label: string;
  value: React.ReactNode;
  hint?: React.ReactNode;
  tone?: HealthTone;
  action?: React.ReactNode;
}) {
  const border =
    tone === "healthy"
      ? "border-green"
      : tone === "degraded"
        ? "border-orange"
        : tone === "critical"
          ? "border-red"
          : tone === "messaging"
            ? "border-purple"
            : "";

  return (
    <div className={`card ${border}`.trim()} style={{ borderRadius: "var(--msf-radius-card)" }}>
      <div className="card-body">
        <div className="d-flex align-items-center">
          <div className="subheader mb-0">{label}</div>
          {action ? <div className="ms-auto">{action}</div> : null}
        </div>
        <div className="h1 mb-0 mt-2">{value}</div>
        {hint ? <div className="text-secondary small mt-1">{hint}</div> : null}
      </div>
    </div>
  );
}

export function PageFrame({
  pretitle,
  title,
  description,
  actions,
  children,
}: {
  pretitle?: string;
  title: string;
  description?: React.ReactNode;
  actions?: React.ReactNode;
  children?: React.ReactNode;
}) {
  return (
    <div className="msf-page-frame">
      <header className="msf-page-frame__header">
        {pretitle ? <div className="page-pretitle">{pretitle}</div> : null}
        <div className="d-flex align-items-start flex-wrap gap-2">
          <div className="min-w-0">
            <h2 className="page-title mb-0">{title}</h2>
            {description ? <div className="msf-page-frame__description mt-1">{description}</div> : null}
          </div>
          {actions ? <div className="ms-auto d-print-none">{actions}</div> : null}
        </div>
      </header>
      {children != null ? <div className="msf-page-frame__body">{children}</div> : null}
    </div>
  );
}

/** @deprecated Prefer PageFrame — kept as alias for gradual migration */
export function SectionHeader({
  title,
  description,
  actions,
}: {
  title: string;
  description?: string;
  actions?: React.ReactNode;
}) {
  return <PageFrame title={title} description={description} actions={actions} />;
}

export function Skeleton({ height = 88, className = "" }: { height?: number; className?: string }) {
  return (
    <div
      className={`placeholder-glow ${className}`.trim()}
      style={{ height, borderRadius: "var(--msf-radius-card)", overflow: "hidden" }}
    >
      <div className="placeholder w-100 h-100" style={{ display: "block", height: "100%" }} />
    </div>
  );
}

export function TableSkeleton({ rows = 5, cols = 4 }: { rows?: number; cols?: number }) {
  return (
    <>
      {Array.from({ length: rows }, (_, row) => (
        <tr key={row} className="msf-table-skeleton">
          {Array.from({ length: cols }, (__, col) => (
            <td key={col}>
              <span className="placeholder placeholder-xs col-8" />
            </td>
          ))}
        </tr>
      ))}
    </>
  );
}

export function DataTableShell({
  title,
  toolbar,
  footer,
  children,
}: {
  title?: string;
  toolbar?: React.ReactNode;
  footer?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <div className="card">
      {title || toolbar ? (
        <div className="card-header">
          {title ? <h3 className="card-title">{title}</h3> : null}
          {toolbar ? <div className="card-actions">{toolbar}</div> : null}
        </div>
      ) : null}
      <div className="table-responsive">{children}</div>
      {footer}
    </div>
  );
}

export function EmptyState({ title, description }: { title: string; description?: string }) {
  return (
    <div className="empty">
      <p className="empty-title">{title}</p>
      {description ? <p className="empty-subtitle text-secondary">{description}</p> : null}
    </div>
  );
}

export function PreviewBanner({ children }: { children?: React.ReactNode }) {
  return (
    <div className="alert alert-azure" role="status">
      <strong>UI preview — awaiting API.</strong>{" "}
      {children ?? "This surface is frontend-only; data is illustrative."}
    </div>
  );
}

export function ForbiddenState({
  permission,
  description,
}: {
  permission?: string | string[];
  description?: string;
}) {
  const label = Array.isArray(permission) ? permission.join(", ") : permission;
  return (
    <div className="msf-forbidden">
      <h2 className="page-title">Access restricted</h2>
      <p className="text-secondary">
        {description ??
          "Your token does not include the permission required for this surface. Re-login after role changes, or ask an administrator."}
      </p>
      {label ? (
        <p className="msf-mono text-secondary mb-3">Required: {label}</p>
      ) : null}
      <a className="btn" href="/">
        Back to Overview
      </a>
    </div>
  );
}

export function ServiceCard({
  name,
  summary,
  status,
  reachable,
  kind,
  actions,
}: {
  name: string;
  summary: string;
  status?: string;
  reachable?: boolean;
  kind?: string;
  actions?: React.ReactNode;
}) {
  return (
    <div className="card card-link-pop h-100" style={{ borderRadius: "var(--msf-radius-card)" }}>
      <div className="card-body">
        <div className="d-flex align-items-center mb-2">
          <HealthIndicator status={status} reachable={reachable} />
          <div className="subheader mb-0 ms-2 text-uppercase">{kind ?? "service"}</div>
          <div className="ms-auto">
            <StatusBadge status={status} reachable={reachable} />
          </div>
        </div>
        <h3 className="card-title mb-1">{name}</h3>
        <p className="text-secondary mb-3">{summary}</p>
        {actions}
      </div>
    </div>
  );
}

export type HealthSummaryItem = {
  id: string;
  label: string;
  status?: string;
  reachable?: boolean;
  hint?: string;
  to?: string;
};

export function HealthSummaryStrip({ items }: { items: HealthSummaryItem[] }) {
  return (
    <div className="row row-cards">
      {items.map((item) => {
        const body = (
          <div className="card h-100">
            <div className="card-body py-3">
              <div className="d-flex align-items-center gap-2">
                <HealthIndicator status={item.status} reachable={item.reachable} />
                <div className="fw-medium text-truncate">{item.label}</div>
                <div className="ms-auto">
                  <StatusBadge status={item.status} reachable={item.reachable} />
                </div>
              </div>
              {item.hint ? <div className="text-secondary small mt-1 text-truncate">{item.hint}</div> : null}
            </div>
          </div>
        );
        return (
          <div className="col-6 col-md-3" key={item.id}>
            {item.to ? (
              <Link to={item.to} className="text-reset text-decoration-none d-block h-100">
                {body}
              </Link>
            ) : (
              body
            )}
          </div>
        );
      })}
    </div>
  );
}

export type ActivityFeedItem = {
  id: string;
  kind: "audit" | "log" | "system";
  title: string;
  meta?: string;
  tone?: HealthTone;
  badge?: string;
  href?: string;
};

export function ActivityFeed({
  title = "Activity",
  items,
  empty,
  actions,
}: {
  title?: string;
  items: ActivityFeedItem[];
  empty?: string;
  actions?: React.ReactNode;
}) {
  return (
    <div className="card h-100">
      <div className="card-header">
        <h3 className="card-title">{title}</h3>
        {actions ? <div className="card-actions">{actions}</div> : null}
      </div>
      <div className="list-group list-group-flush">
        {items.length === 0 ? (
          <div className="list-group-item text-secondary">{empty ?? "No recent activity."}</div>
        ) : (
          items.map((item) => {
            const content = (
              <>
                <div className="d-flex align-items-center gap-2">
                  {item.badge ? <StatusBadge tone={item.tone ?? "info"}>{item.badge}</StatusBadge> : null}
                  <div className="fw-medium text-truncate">{item.title}</div>
                </div>
                {item.meta ? <div className="text-secondary small mt-1 text-truncate">{item.meta}</div> : null}
              </>
            );
            return item.href ? (
              <Link key={item.id} className="list-group-item list-group-item-action" to={item.href}>
                {content}
              </Link>
            ) : (
              <div className="list-group-item" key={item.id}>
                {content}
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}

export type StepFlowStatus = "pending" | "active" | "done" | "failed" | "compensated";

export type StepFlowStep = {
  id: string;
  label: string;
  detail?: string;
  status?: StepFlowStatus;
};

export function StepFlow({
  steps,
  compact,
}: {
  steps: StepFlowStep[];
  compact?: boolean;
}) {
  return (
    <div className={`msf-saga-flow ${compact ? "msf-saga-flow--compact" : ""}`}>
      {steps.map((step, index) => (
        <div key={step.id} className="d-flex align-items-center gap-2">
          <div
            className={`msf-saga-step msf-saga-step--${step.status ?? "pending"}`}
            title={step.detail}
          >
            <div>{step.label}</div>
            {step.detail && !compact ? (
              <div className="msf-saga-step__detail">{step.detail}</div>
            ) : null}
          </div>
          {index < steps.length - 1 ? <span className="msf-saga-arrow">→</span> : null}
        </div>
      ))}
    </div>
  );
}

export type TimelineItem = {
  id: string;
  at: string;
  title: string;
  detail?: string;
  tone?: HealthTone;
};

export function Timeline({ items, empty }: { items: TimelineItem[]; empty?: string }) {
  if (items.length === 0) {
    return <div className="text-secondary">{empty ?? "No timeline events."}</div>;
  }

  return (
    <ol className="msf-timeline">
      {items.map((item) => (
        <li className="msf-timeline__item" key={item.id}>
          <span
            className="msf-timeline__dot"
            style={{ background: toneColor[item.tone ?? "info"] }}
          />
          <div className="msf-timeline__body">
            <div className="d-flex align-items-baseline gap-2 flex-wrap">
              <time className="text-secondary small">{item.at}</time>
              <span className="fw-medium">{item.title}</span>
            </div>
            {item.detail ? <div className="text-secondary small mt-1">{item.detail}</div> : null}
          </div>
        </li>
      ))}
    </ol>
  );
}

export function WorkflowCard({
  title,
  count,
  hint,
  to,
  tone,
}: {
  title: string;
  count: number | string;
  hint?: string;
  to?: string;
  tone?: HealthTone;
}) {
  const body = (
    <div className="card h-100">
      <div className="card-body">
        <div className="subheader">{hint ?? "Board"}</div>
        <h3 className="card-title">{title}</h3>
        <div className="h1 mb-0">{count}</div>
        {tone ? (
          <div className="mt-2">
            <StatusBadge tone={tone}>{title}</StatusBadge>
          </div>
        ) : null}
      </div>
    </div>
  );

  return to ? (
    <Link to={to} className="text-reset text-decoration-none d-block h-100">
      {body}
    </Link>
  ) : (
    body
  );
}

/** Shell for future Prometheus-backed charts — honest empty until metrics BFF */
export function MetricChartShell({
  title,
  hint = "Awaiting metrics BFF — open Grafana for live series.",
  height = 180,
}: {
  title: string;
  hint?: string;
  height?: number;
}) {
  return (
    <div className="card h-100">
      <div className="card-header">
        <h3 className="card-title">{title}</h3>
      </div>
      <div
        className="card-body d-flex align-items-center justify-content-center text-secondary text-center"
        style={{ minHeight: height }}
      >
        <div>
          <div className="mb-1">Chart placeholder</div>
          <div className="small">{hint}</div>
        </div>
      </div>
    </div>
  );
}

export function LogViewer({
  children,
  footer,
  loading,
  empty,
}: {
  children: React.ReactNode;
  footer?: React.ReactNode;
  loading?: boolean;
  empty?: boolean;
}) {
  return (
    <div className={`card msf-log-viewer ${loading ? "msf-log-viewer--loading" : ""} ${empty ? "msf-log-viewer--empty" : ""}`}>
      <div className="table-responsive">{children}</div>
      {footer}
    </div>
  );
}

export function CopyCommandBlock({
  command,
  label = "Copy commands",
}: {
  command: string;
  label?: string;
}) {
  const toast = useToast();
  const [copied, setCopied] = useState(false);

  async function copy() {
    await navigator.clipboard.writeText(command);
    setCopied(true);
    toast.success("Command copied to clipboard.");
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <div>
      <pre className="bg-dark text-light p-3 rounded mb-2" style={{ whiteSpace: "pre-wrap" }}>
        {command}
      </pre>
      <button type="button" className="btn btn-primary" onClick={() => void copy()}>
        {copied ? "Copied" : label}
      </button>
    </div>
  );
}
