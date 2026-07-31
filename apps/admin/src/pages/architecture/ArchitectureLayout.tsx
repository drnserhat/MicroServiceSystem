import { Link, Outlet } from "react-router-dom";
import { HubTabs, PageFrame } from "@/components/control";

const TABS = [
  { to: "/architecture", label: "Overview", end: true },
  { to: "/architecture/contexts", label: "Bounded contexts" },
  { to: "/architecture/dependencies", label: "Dependencies" },
  { to: "/architecture/events", label: "Events" },
  { to: "/architecture/event-flow", label: "Event flow" },
  { to: "/architecture/databases", label: "Databases" },
  { to: "/architecture/contracts", label: "Contracts" },
] as const;

export function ArchitectureLayout() {
  return (
    <PageFrame
      pretitle="Architecture"
      title="Architecture Explorer"
      description="Design-time topology — bounded contexts, contracts, and data ownership. Runtime health lives on Platform Map."
      actions={
        <div className="btn-list">
          <Link className="btn" to="/map">
            Platform Map
          </Link>
          <Link className="btn" to="/building-blocks">
            BuildingBlocks
          </Link>
        </div>
      }
    >
      <HubTabs tabs={TABS} label="Architecture sections" />
      <Outlet />
    </PageFrame>
  );
}
