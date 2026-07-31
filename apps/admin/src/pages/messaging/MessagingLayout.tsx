import { Outlet } from "react-router-dom";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { HubTabs, PageFrame } from "@/components/control";
import { ExternalToolLink } from "@/platform/tools";

const TABS = [
  { to: "/messaging", label: "Overview", end: true },
  { to: "/messaging/queues", label: "Queues" },
  { to: "/messaging/exchanges", label: "Exchanges" },
  { to: "/messaging/bindings", label: "Bindings" },
  { to: "/messaging/publishers", label: "Publishers" },
  { to: "/messaging/consumers", label: "Consumers" },
  { to: "/messaging/dead-letters", label: "Dead letters" },
  { to: "/messaging/outbox", label: "Outbox" },
  { to: "/messaging/inbox", label: "Inbox" },
  { to: "/messaging/event-flow", label: "Event flow" },
  { to: "/messaging/retries", label: "Retries" },
  { to: "/messaging/replay", label: "Replay" },
  { to: "/messaging/inspect", label: "Inspect" },
  { to: "/messaging/timeline", label: "Timeline" },
] as const;

export function MessagingLayout() {
  return (
    <RequirePermission permission={FrameworkPermissions.OpsOutboxRead}>
      <PageFrame
        pretitle="Operations"
        title="Messaging Center"
        description="Rabbit-inspired control plane — live per-service outbox/DLQ and inbox counts; topology remains preview."
        actions={
          <div className="btn-list">
            <ExternalToolLink id="rabbitmq" className="btn" />
          </div>
        }
      >
        <HubTabs tabs={TABS} label="Messaging sections" />
        <Outlet />
      </PageFrame>
    </RequirePermission>
  );
}
