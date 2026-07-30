import { useEffect, useMemo, useState } from "react";
import { Link, NavLink, Outlet, useLocation } from "react-router-dom";
import {
  IconBrandGithub,
  IconLayoutSidebar,
  IconLogout,
  IconMoon,
  IconSearch,
  IconSun,
} from "@tabler/icons-react";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { useTheme } from "@/theme/ThemeContext";
import { displayNameFromEmail, initialsFromName } from "./displayName";
import { CommandPalette } from "./CommandPalette";
import { NAV_SECTIONS } from "./navConfig";
import "./controlCenter.css";

const FAVORITES_KEY = "msf.admin.favorites";
const RECENT_KEY = "msf.admin.recent";

function loadList(key: string): string[] {
  try {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

export function AppLayout() {
  const { session, logout, can } = useAuth();
  const { colorMode, setColorMode, toggleColorMode } = useTheme();
  const location = useLocation();
  const displayName = displayNameFromEmail(session?.email);
  const initials = initialsFromName(displayName);
  const tenantShort = session?.tenantId?.slice(0, 8) ?? "—";

  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [shortcutsOpen, setShortcutsOpen] = useState(false);
  const [favorites, setFavorites] = useState<string[]>(() => loadList(FAVORITES_KEY));
  const [recent, setRecent] = useState<string[]>(() => loadList(RECENT_KEY));

  useEffect(() => {
    function onOpen() {
      setPaletteOpen(true);
    }
    function onKey(event: KeyboardEvent) {
      if (event.key === "?" && !event.ctrlKey && !event.metaKey && !event.altKey) {
        const tag = (event.target as HTMLElement | null)?.tagName;
        if (tag === "INPUT" || tag === "TEXTAREA" || (event.target as HTMLElement)?.isContentEditable) {
          return;
        }
        event.preventDefault();
        setShortcutsOpen(true);
      }
    }
    document.addEventListener("msf:open-command-palette", onOpen);
    window.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("msf:open-command-palette", onOpen);
      window.removeEventListener("keydown", onKey);
    };
  }, []);

  useEffect(() => {
    setMobileOpen(false);
    setRecent((prev) => {
      const next = [location.pathname, ...prev.filter((p) => p !== location.pathname)].slice(0, 8);
      localStorage.setItem(RECENT_KEY, JSON.stringify(next));
      return next;
    });
  }, [location.pathname]);

  const crumbs = useMemo(() => {
    const parts = location.pathname.split("/").filter(Boolean);
    if (parts.length === 0) return [{ label: "Platform Overview", to: "/" }];
    const acc: { label: string; to: string }[] = [{ label: "Overview", to: "/" }];
    let path = "";
    for (const part of parts) {
      path += `/${part}`;
      acc.push({ label: part, to: path });
    }
    return acc;
  }, [location.pathname]);

  function toggleFavorite(path: string) {
    setFavorites((prev) => {
      const next = prev.includes(path) ? prev.filter((p) => p !== path) : [...prev, path].slice(0, 12);
      localStorage.setItem(FAVORITES_KEY, JSON.stringify(next));
      return next;
    });
  }

  const shellClass = [
    "msf-cc-shell",
    collapsed ? "is-collapsed" : "",
    mobileOpen ? "is-sidebar-open" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={shellClass}>
      <aside className="msf-cc-sidebar">
        <div className="msf-cc-sidebar-brand d-flex align-items-center gap-2">
          <Link to="/" className="text-reset text-decoration-none">
            MSF Control
          </Link>
        </div>
        <div className="msf-cc-sidebar-meta px-3 pb-2 small text-secondary">
          Tenant {tenantShort}… · {can(FrameworkPermissions.RegistrationUsersCreate) ? "Admin" : "Member"}
        </div>
        <nav className="msf-cc-sidebar-nav">
          {favorites.length > 0 ? (
            <div className="msf-cc-nav-section">
              <div className="msf-cc-nav-section-label">Favorites</div>
              {favorites.map((fav) => (
                <NavLink key={fav} to={fav} className={({ isActive }) => (isActive ? "msf-cc-nav-link active" : "msf-cc-nav-link")}>
                  <span className="msf-cc-nav-label">{fav}</span>
                </NavLink>
              ))}
            </div>
          ) : null}
          {NAV_SECTIONS.map((section) => {
            const items = section.items.filter((item) => !item.permission || can(item.permission));
            if (items.length === 0) return null;
            return (
              <div className="msf-cc-nav-section" key={section.id}>
                <div className="msf-cc-nav-section-label">{section.label}</div>
                {items.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    end={item.end}
                    title={item.label}
                    className={({ isActive }) => (isActive ? "msf-cc-nav-link active" : "msf-cc-nav-link")}
                  >
                    <span className="text-secondary">{item.icon}</span>
                    <span className="msf-cc-nav-label">{item.label}</span>
                  </NavLink>
                ))}
              </div>
            );
          })}
        </nav>
      </aside>

      <div className="msf-cc-main">
        <header className="msf-cc-topbar">
          <button
            type="button"
            className="btn btn-ghost-secondary btn-icon"
            title="Toggle sidebar"
            onClick={() => {
              if (window.innerWidth < 992) setMobileOpen((v) => !v);
              else setCollapsed((v) => !v);
            }}
          >
            <IconLayoutSidebar size={20} stroke={1.5} />
          </button>
          <button
            type="button"
            className="btn btn-ghost-secondary"
            onClick={() => setPaletteOpen(true)}
            title="Command palette (Ctrl+K)"
          >
            <IconSearch size={16} stroke={1.5} className="me-1" />
            Search
            <kbd className="ms-2 small">Ctrl K</kbd>
          </button>
          <div className="msf-cc-breadcrumb d-none d-md-flex align-items-center gap-1">
            {crumbs.map((crumb, index) => (
              <span key={crumb.to}>
                {index > 0 ? <span className="mx-1">/</span> : null}
                <Link to={crumb.to} className="text-secondary text-decoration-none">
                  {crumb.label}
                </Link>
              </span>
            ))}
          </div>
          <div className="ms-auto d-flex align-items-center gap-2">
            <button
              type="button"
              className="btn btn-ghost-secondary btn-sm"
              onClick={() => setShortcutsOpen(true)}
              title="Keyboard shortcuts (?)"
            >
              ?
            </button>
            <button
              type="button"
              className="btn btn-ghost-secondary btn-sm"
              onClick={() => toggleFavorite(location.pathname)}
              title="Toggle favorite"
            >
              {favorites.includes(location.pathname) ? "★" : "☆"}
            </button>
            <a
              href="https://github.com/drnserhat/MicroServiceSystem"
              className="btn btn-ghost-secondary btn-icon"
              target="_blank"
              rel="noreferrer"
              title="Source"
            >
              <IconBrandGithub size={18} stroke={1.5} />
            </a>
            <button
              type="button"
              className="btn btn-ghost-secondary btn-icon"
              title="Toggle theme"
              onClick={() => (colorMode === "dark" ? setColorMode("light") : setColorMode("dark"))}
            >
              {colorMode === "dark" ? <IconSun size={18} stroke={1.5} /> : <IconMoon size={18} stroke={1.5} />}
            </button>
            <div className="dropdown">
              <a href="#user" className="nav-link d-flex lh-1 p-0" data-bs-toggle="dropdown">
                <span className="avatar avatar-sm bg-primary-lt">{initials}</span>
                <div className="d-none d-xl-block ps-2">
                  <div>{displayName}</div>
                  <div className="mt-1 small text-secondary">Control Center</div>
                </div>
              </a>
              <div className="dropdown-menu dropdown-menu-end">
                <div className="dropdown-item-text small text-secondary">Recent</div>
                {recent.slice(0, 5).map((path) => (
                  <Link key={path} className="dropdown-item" to={path}>
                    {path}
                  </Link>
                ))}
                <div className="dropdown-divider" />
                <button type="button" className="dropdown-item" onClick={toggleColorMode}>
                  Theme: {colorMode}
                </button>
                <button type="button" className="dropdown-item" onClick={logout}>
                  <IconLogout size={16} stroke={1.5} className="me-2" />
                  Logout
                </button>
              </div>
            </div>
          </div>
        </header>
        <div className="msf-cc-content">
          <Outlet />
        </div>
      </div>

      <CommandPalette open={paletteOpen} onClose={() => setPaletteOpen(false)} can={can} />

      {shortcutsOpen ? (
        <div
          className="modal modal-blur fade show d-block"
          style={{ background: "rgba(0,0,0,.55)" }}
          role="dialog"
          onClick={() => setShortcutsOpen(false)}
        >
          <div className="modal-dialog modal-dialog-centered" role="document" onClick={(e) => e.stopPropagation()}>
            <div className="modal-content">
              <div className="modal-header">
                <h3 className="modal-title">Keyboard shortcuts</h3>
                <button type="button" className="btn-close" onClick={() => setShortcutsOpen(false)} />
              </div>
              <div className="modal-body">
                <div className="datagrid">
                  <div className="datagrid-item">
                    <div className="datagrid-title">Command palette</div>
                    <div className="datagrid-content">
                      <kbd>Ctrl</kbd> + <kbd>K</kbd>
                    </div>
                  </div>
                  <div className="datagrid-item">
                    <div className="datagrid-title">Shortcuts help</div>
                    <div className="datagrid-content">
                      <kbd>?</kbd>
                    </div>
                  </div>
                  <div className="datagrid-item">
                    <div className="datagrid-title">Close modal</div>
                    <div className="datagrid-content">
                      <kbd>Esc</kbd>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
