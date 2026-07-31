import { Link } from "react-router-dom";
import type { HealthTone } from "./tones";
import { badgeClass } from "./tones";

function ToneBadge({ tone, children }: { tone: HealthTone; children: React.ReactNode }) {
  return <span className={badgeClass(tone)}>{children}</span>;
}

/** Rabbit-style queue / exchange summary card */
export function QueueCard({
  title,
  count,
  hint,
  to,
  ready,
  unacked,
  consumers,
  preview,
}: {
  title: string;
  count?: number | string;
  hint?: string;
  to?: string;
  ready?: number | string;
  unacked?: number | string;
  consumers?: number | string;
  preview?: boolean;
}) {
  const body = (
    <div className="card h-100">
      <div className="card-body">
        <div className="d-flex align-items-center gap-2 mb-1">
          <div className="subheader mb-0">{hint ?? "Queue"}</div>
          {preview ? <ToneBadge tone="messaging">preview</ToneBadge> : null}
        </div>
        <h3 className="card-title">{title}</h3>
        {count != null ? <div className="h1 mb-2">{count}</div> : null}
        {(ready != null || unacked != null || consumers != null) && (
          <div className="d-flex flex-wrap gap-3 text-secondary small">
            {ready != null ? <span>Ready {ready}</span> : null}
            {unacked != null ? <span>Unacked {unacked}</span> : null}
            {consumers != null ? <span>Consumers {consumers}</span> : null}
          </div>
        )}
        {to ? (
          <div className="mt-3">
            <span className="btn btn-sm">Browse</span>
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

export function EventCard({
  eventName,
  service,
  meta,
  tone = "messaging",
  actions,
  onSelect,
}: {
  eventName: string;
  service?: string;
  meta?: string;
  tone?: HealthTone;
  actions?: React.ReactNode;
  onSelect?: () => void;
}) {
  const inner = (
    <>
      <div className="d-flex align-items-center gap-2">
        <ToneBadge tone={tone}>{service ?? "event"}</ToneBadge>
        <code className="text-truncate">{eventName}</code>
      </div>
      {meta ? <div className="text-secondary small mt-1 text-truncate">{meta}</div> : null}
      {actions ? <div className="mt-2">{actions}</div> : null}
    </>
  );

  if (onSelect) {
    return (
      <button type="button" className="card w-100 text-start msf-event-card" onClick={onSelect}>
        <div className="card-body py-3">{inner}</div>
      </button>
    );
  }

  return (
    <div className="card">
      <div className="card-body py-3">{inner}</div>
    </div>
  );
}

export function DetailDrawer({
  open,
  title,
  onClose,
  children,
  footer,
  width = 420,
}: {
  open: boolean;
  title: string;
  onClose: () => void;
  children: React.ReactNode;
  footer?: React.ReactNode;
  width?: number;
}) {
  if (!open) return null;

  return (
    <div className="msf-drawer-root">
      <button type="button" className="msf-drawer-backdrop" aria-label="Close drawer" onClick={onClose} />
      <aside
        className="msf-drawer"
        style={{ width }}
        role="dialog"
        aria-modal="true"
        aria-label={title}
      >
        <div className="msf-drawer__header">
          <h3 className="msf-drawer__title">{title}</h3>
          <button type="button" className="btn-close" aria-label="Close" onClick={onClose} />
        </div>
        <div className="msf-drawer__body">{children}</div>
        {footer ? <div className="msf-drawer__footer">{footer}</div> : null}
      </aside>
    </div>
  );
}
