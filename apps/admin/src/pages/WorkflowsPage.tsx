import { Link } from "react-router-dom";
import { PreviewBanner, SectionHeader, StatusBadge } from "@/components/control";

const STEPS = ["Identity", "User", "Notification", "Audit", "Completed"] as const;

const PREVIEW_SAGAS = [
  { id: "saga-1", name: "RegisterUser", state: "Completed", duration: "1.2s" },
  { id: "saga-2", name: "RegisterUser", state: "Running", duration: "—" },
  { id: "saga-3", name: "RegisterUser", state: "Compensating", duration: "0.8s" },
];

export function WorkflowsPage() {
  return (
    <>
      <SectionHeader
        title="Workflow Center"
        description="Coordinator orchestration (RegisterUser saga). Live saga queries are not exposed yet."
        actions={
          <Link className="btn btn-primary" to="/users/register">
            Start registration
          </Link>
        }
      />
      <PreviewBanner>
        Aligns with docs/distributed-workflows.md — orchestration in Coordinator, choreography via outbox events.
      </PreviewBanner>

      <div className="card mb-3">
        <div className="card-header">
          <h3 className="card-title">RegisterUser flow</h3>
        </div>
        <div className="card-body">
          <div className="msf-saga-flow">
            {STEPS.map((step, index) => (
              <div key={step} className="d-flex align-items-center gap-2">
                <div className="msf-saga-step">{step}</div>
                {index < STEPS.length - 1 ? <span className="msf-saga-arrow">→</span> : null}
              </div>
            ))}
          </div>
          <p className="text-secondary mt-3 mb-0">
            On profile failure, Coordinator compensates with Identity disable (internal API key). Welcome + Audit
            publish through the transactional outbox.
          </p>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Saga instances</h3>
        </div>
        <div className="table-responsive">
          <table className="table table-vcenter card-table">
            <thead>
              <tr>
                <th>Saga</th>
                <th>State</th>
                <th>Duration</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {PREVIEW_SAGAS.map((saga) => (
                <tr key={saga.id}>
                  <td>
                    <code>{saga.name}</code>
                  </td>
                  <td>
                    <StatusBadge
                      status={saga.state}
                      tone={
                        saga.state === "Completed"
                          ? "healthy"
                          : saga.state === "Running"
                            ? "info"
                            : "degraded"
                      }
                    />
                  </td>
                  <td className="text-secondary">{saga.duration}</td>
                  <td>
                    <button type="button" className="btn btn-sm" disabled>
                      Inspect
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
