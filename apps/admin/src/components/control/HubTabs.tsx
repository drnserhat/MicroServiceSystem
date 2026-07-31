import { NavLink } from "react-router-dom";

export type HubTab = {
  to: string;
  label: string;
  end?: boolean;
};

/** Shared hub tab strip with basic a11y for nested centers */
export function HubTabs({ tabs, label }: { tabs: readonly HubTab[]; label: string }) {
  return (
    <ul className="nav nav-tabs mb-3 flex-nowrap overflow-auto" role="tablist" aria-label={label}>
      {tabs.map((tab) => (
        <li className="nav-item" key={tab.to} role="presentation">
          <NavLink
            to={tab.to}
            end={tab.end ?? false}
            role="tab"
            className={({ isActive }) => `nav-link ${isActive ? "active" : ""}`}
          >
            {tab.label}
          </NavLink>
        </li>
      ))}
    </ul>
  );
}
