import { Link } from "react-router-dom";
import {
  DataTableShell,
  HealthIndicator,
  PreviewBanner,
  StepFlow,
  StatusBadge,
} from "@/components/control";
import {
  ARCH_CANVAS_ROWS,
  ARCH_DEPENDENCIES,
  BOUNDED_CONTEXTS,
  DB_OWNERSHIP,
  INTEGRATION_EVENTS,
} from "./catalog";

export function ArchitectureOverviewPage() {
  return (
    <>
      <PreviewBanner>
        Design-time canvas (navigational). For live health overlays use{" "}
        <Link to="/map">Platform Map</Link>. Dependency edges here are conceptual.
      </PreviewBanner>

      <div className="msf-arch-canvas mb-3">
        {ARCH_CANVAS_ROWS.map((row, rowIndex) => (
          <div key={rowIndex}>
            <div className="msf-arch-row">
              {row.map((node) => {
                const body = (
                  <div className="msf-arch-node">
                    <div className="d-flex justify-content-center mb-1">
                      <HealthIndicator status="Healthy" />
                    </div>
                    <div className="fw-bold">{node.label}</div>
                    <div className="text-secondary small">{node.meta}</div>
                  </div>
                );
                return node.to ? (
                  <Link key={node.id} to={node.to} className="text-reset text-decoration-none">
                    {body}
                  </Link>
                ) : (
                  <div key={node.id}>{body}</div>
                );
              })}
            </div>
            {rowIndex < ARCH_CANVAS_ROWS.length - 1 ? (
              <div className="text-center text-secondary my-1" aria-hidden>
                ↓
              </div>
            ) : null}
          </div>
        ))}
      </div>

      <div className="btn-list">
        <Link className="btn btn-primary" to="/architecture/contexts">
          Bounded contexts
        </Link>
        <Link className="btn" to="/services">
          Service Center
        </Link>
        <Link className="btn" to="/building-blocks">
          BuildingBlocks
        </Link>
      </div>
    </>
  );
}

export function ArchitectureContextsPage() {
  return (
    <>
      <PreviewBanner>Static inventory aligned with repo services — not a live discovery API.</PreviewBanner>
      <DataTableShell title="Bounded contexts">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Context</th>
              <th>Owns</th>
              <th>DB</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {BOUNDED_CONTEXTS.map((ctx) => (
              <tr key={ctx.id}>
                <td className="fw-medium">{ctx.name}</td>
                <td className="text-secondary">{ctx.owns}</td>
                <td>
                  <code className="small">{ctx.db}</code>
                </td>
                <td>
                  {ctx.to ? (
                    <Link className="btn btn-sm" to={ctx.to}>
                      Open
                    </Link>
                  ) : null}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
    </>
  );
}

export function ArchitectureDependenciesPage() {
  return (
    <>
      <PreviewBanner>
        Conceptual call/event edges. Runtime graph with health: <Link to="/map">Platform Map</Link>.
      </PreviewBanner>
      <DataTableShell title="Dependencies">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>From</th>
              <th>To</th>
              <th>Via</th>
            </tr>
          </thead>
          <tbody>
            {ARCH_DEPENDENCIES.map((dep, i) => (
              <tr key={i}>
                <td>{dep.from}</td>
                <td>{dep.to}</td>
                <td className="text-secondary">{dep.via}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
    </>
  );
}

export function ArchitectureEventsPage() {
  return (
    <>
      <PreviewBanner>Integration events owned by publishers — contracts under Shared/Contracts.</PreviewBanner>
      <DataTableShell title="Integration events">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Event</th>
              <th>Owner</th>
              <th>Contract</th>
              <th>Note</th>
            </tr>
          </thead>
          <tbody>
            {INTEGRATION_EVENTS.map((evt) => (
              <tr key={evt.name}>
                <td>
                  <code>{evt.name}</code>
                </td>
                <td>{evt.owner}</td>
                <td className="text-secondary">{evt.contract}</td>
                <td className="text-secondary">{evt.note ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
      <div className="mt-3">
        <Link className="btn" to="/messaging/event-flow">
          Messaging event flow
        </Link>
      </div>
    </>
  );
}

export function ArchitectureEventFlowPage() {
  return (
    <>
      <PreviewBanner>
        Choreography path after orchestration hands off — see docs/distributed-workflows.md.
      </PreviewBanner>
      <div className="card mb-3">
        <div className="card-body">
          <StepFlow
            steps={[
              { id: "1", label: "Outbox write", detail: "Same UoW as domain", status: "done" },
              { id: "2", label: "Relay", detail: "RabbitMQ publish", status: "done" },
              { id: "3", label: "Consumers", detail: "Inbox idempotency", status: "done" },
              { id: "4", label: "Projections", detail: "Audit / Notification / …", status: "done" },
            ]}
          />
        </div>
      </div>
      <div className="btn-list">
        <Link className="btn" to="/messaging/event-flow">
          Messaging center
        </Link>
        <Link className="btn" to="/workflows/definitions">
          RegisterUser saga
        </Link>
      </div>
    </>
  );
}

export function ArchitectureDatabasesPage() {
  return (
    <>
      <PreviewBanner>Database-per-context — no shared write DB across bounded contexts.</PreviewBanner>
      <DataTableShell title="Database ownership">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Store</th>
              <th>Owners</th>
              <th>Note</th>
            </tr>
          </thead>
          <tbody>
            {DB_OWNERSHIP.map((row) => (
              <tr key={row.store}>
                <td>{row.store}</td>
                <td>
                  <div className="d-flex flex-wrap gap-1">
                    {row.owners.map((o) => (
                      <StatusBadge key={o} tone="infra">
                        {o}
                      </StatusBadge>
                    ))}
                  </div>
                </td>
                <td className="text-secondary">{row.note}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
    </>
  );
}

export function ArchitectureContractsPage() {
  return (
    <>
      <PreviewBanner>
        Contracts package is the shared language for integration events — version carefully.
      </PreviewBanner>
      <div className="card mb-3">
        <div className="card-body">
          <h3 className="card-title">Shared/Contracts</h3>
          <p className="text-secondary">
            Event DTOs consumed by publishers and consumers. Prefer additive evolution; avoid
            breaking payload renames without dual-publish windows.
          </p>
          <Link className="btn" to="/building-blocks">
            Contracts building block
          </Link>
          <Link className="btn ms-2" to="/developer/event">
            Create event wizard
          </Link>
        </div>
      </div>
      <DataTableShell title="Published contracts (preview)">
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Type</th>
              <th>Owner</th>
            </tr>
          </thead>
          <tbody>
            {INTEGRATION_EVENTS.map((evt) => (
              <tr key={evt.name}>
                <td>
                  <code>{evt.name}</code>
                </td>
                <td>{evt.owner}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
    </>
  );
}
