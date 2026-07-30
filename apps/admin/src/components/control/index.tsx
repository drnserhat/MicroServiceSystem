import type { HealthTone } from "./tones";
import { badgeClass, toneFromStatus } from "./tones";

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
  const color =
    tone === "healthy"
      ? "var(--tblr-green)"
      : tone === "degraded"
        ? "var(--tblr-orange)"
        : tone === "critical"
          ? "var(--tblr-red)"
          : "var(--tblr-secondary)";

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
    <div className={`card ${border}`.trim()}>
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

export function SectionHeader({
  title,
  description,
  actions,
}: {
  title: string;
  description?: string;
  actions?: React.ReactNode;
}) {
  return (
    <div className="d-flex align-items-start flex-wrap gap-2 mb-3">
      <div>
        <h2 className="page-title mb-1">{title}</h2>
        {description ? <div className="text-secondary">{description}</div> : null}
      </div>
      {actions ? <div className="ms-auto">{actions}</div> : null}
    </div>
  );
}

export function Skeleton({ height = 88, className = "" }: { height?: number; className?: string }) {
  return (
    <div
      className={`placeholder-glow ${className}`.trim()}
      style={{ height, borderRadius: 8, overflow: "hidden" }}
    >
      <div className="placeholder w-100 h-100" style={{ display: "block", height: "100%" }} />
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
    <div className="card card-link-pop h-100">
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
