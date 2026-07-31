import { Link, Outlet } from "react-router-dom";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { HubTabs, PageFrame } from "@/components/control";

const TABS = [
  { to: "/workflows", label: "Overview", end: true },
  { to: "/workflows/boards", label: "Boards" },
  { to: "/workflows/definitions", label: "Definitions" },
  { to: "/workflows/running", label: "Running" },
  { to: "/workflows/completed", label: "Completed" },
  { to: "/workflows/failed", label: "Failed" },
  { to: "/workflows/compensated", label: "Compensated" },
  { to: "/workflows/waiting", label: "Waiting" },
  { to: "/workflows/retrying", label: "Retrying" },
] as const;

export function WorkflowsLayout() {
  return (
    <RequirePermission permission={FrameworkPermissions.OpsSagaRead}>
      <PageFrame
        pretitle="Operations"
        title="Workflow Center"
        description="Coordinator RegisterUser saga — live list/detail via ops.saga.read. Definitions stay educational."
        actions={
          <div className="btn-list">
            <Link className="btn" to="/messaging/outbox?service=coordinator">
              Outbox
            </Link>
            <Link className="btn btn-primary" to="/users/register">
              Start registration
            </Link>
          </div>
        }
      >
        <HubTabs tabs={TABS} label="Workflow sections" />
        <Outlet />
      </PageFrame>
    </RequirePermission>
  );
}
