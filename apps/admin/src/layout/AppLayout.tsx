import { useEffect, useMemo, useState } from "react";
import { Link, NavLink, Outlet, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
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
import { LanguageSwitcher } from "@/i18n/LanguageSwitcher";
import { useTheme } from "@/theme/ThemeContext";
import { displayNameFromEmail, initialsFromName } from "./displayName";
import { CommandPalette } from "./CommandPalette";
import { NAV_SECTIONS, resolveBreadcrumbs, resolvePathLabel } from "./navConfig";
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
  const { t } = useTranslation(["common", "nav"]);
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

  const crumbs = useMemo(
    () => resolveBreadcrumbs(location.pathname, t),
    [location.pathname, t],
  );

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

  const roleLabel = can(FrameworkPermissions.RegistrationUsersCreate) ? t("admin") : t("member");

  return (
    <div className={shellClass}>
      <aside className="msf-cc-sidebar">
        <div className="msf-cc-sidebar-brand d-flex align-items-center gap-2">
          <Link to="/" className="text-reset text-decoration-none">
            {t("brandShort")}
          </Link>
        </div>
        <div className="msf-cc-sidebar-meta px-3 pb-2 small text-secondary">
          {t("tenant")} {tenantShort}… · {roleLabel}
        </div>
        <nav className="msf-cc-sidebar-nav">
          {favorites.length > 0 ? (
            <div className="msf-cc-nav-section">
              <div className="msf-cc-nav-section-label">{t("favorites")}</div>
              {favorites.map((fav) => (
                <NavLink key={fav} to={fav} className={({ isActive }) => (isActive ? "msf-cc-nav-link active" : "msf-cc-nav-link")} title={fav}>
                  <span className="msf-cc-nav-label">{resolvePathLabel(fav, t)}</span>
                </NavLink>
              ))}
            </div>
          ) : null}
          {NAV_SECTIONS.map((section) => {
            const items = section.items.filter((item) => !item.permission || can(item.permission));
            if (items.length === 0) return null;
            return (
              <div className="msf-cc-nav-section" key={section.id}>
                <div className="msf-cc-nav-section-label">{t(`nav:${section.labelKey}`)}</div>
                {items.map((item) => {
                  const label = t(`nav:${item.labelKey}`);
                  return (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      end={item.end}
                      title={label}
                      className={({ isActive }) => (isActive ? "msf-cc-nav-link active" : "msf-cc-nav-link")}
                    >
                      <span className="text-secondary">{item.icon}</span>
                      <span className="msf-cc-nav-label">{label}</span>
                    </NavLink>
                  );
                })}
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
            title={t("toggleSidebar")}
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
            title={`${t("shortcutPalette")} (Ctrl+K)`}
          >
            <IconSearch size={16} stroke={1.5} className="me-1" />
            {t("search")}
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
            <LanguageSwitcher />
            <button
              type="button"
              className="btn btn-ghost-secondary btn-sm"
              onClick={() => setShortcutsOpen(true)}
              title={t("shortcutHelp")}
            >
              ?
            </button>
            <button
              type="button"
              className="btn btn-ghost-secondary btn-sm"
              onClick={() => toggleFavorite(location.pathname)}
              title={t("toggleFavorite")}
            >
              {favorites.includes(location.pathname) ? "★" : "☆"}
            </button>
            <a
              href="https://github.com/drnserhat/MicroServiceSystem"
              className="btn btn-ghost-secondary btn-icon"
              target="_blank"
              rel="noreferrer"
              title={t("source")}
            >
              <IconBrandGithub size={18} stroke={1.5} />
            </a>
            <button
              type="button"
              className="btn btn-ghost-secondary btn-icon"
              title={t("toggleTheme")}
              onClick={() => (colorMode === "dark" ? setColorMode("light") : setColorMode("dark"))}
            >
              {colorMode === "dark" ? <IconSun size={18} stroke={1.5} /> : <IconMoon size={18} stroke={1.5} />}
            </button>
            <div className="dropdown">
              <a href="#user" className="nav-link d-flex lh-1 p-0" data-bs-toggle="dropdown">
                <span className="avatar avatar-sm bg-primary-lt">{initials}</span>
                <div className="d-none d-xl-block ps-2">
                  <div>{displayName}</div>
                  <div className="mt-1 small text-secondary">{t("controlCenter")}</div>
                </div>
              </a>
              <div className="dropdown-menu dropdown-menu-end">
                <div className="dropdown-item-text small text-secondary">{t("recent")}</div>
                {recent.slice(0, 5).map((path) => (
                  <Link key={path} className="dropdown-item" to={path} title={path}>
                    {resolvePathLabel(path, t)}
                  </Link>
                ))}
                <div className="dropdown-divider" />
                <button type="button" className="dropdown-item" onClick={toggleColorMode}>
                  {t("theme")}: {colorMode === "dark" ? t("themeDark") : t("themeLight")}
                </button>
                <button type="button" className="dropdown-item" onClick={logout}>
                  <IconLogout size={16} stroke={1.5} className="me-2" />
                  {t("logout")}
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
                <h3 className="modal-title">{t("shortcutsTitle")}</h3>
                <button type="button" className="btn-close" onClick={() => setShortcutsOpen(false)} />
              </div>
              <div className="modal-body">
                <div className="datagrid">
                  <div className="datagrid-item">
                    <div className="datagrid-title">{t("shortcutPalette")}</div>
                    <div className="datagrid-content">
                      <kbd>Ctrl</kbd> + <kbd>K</kbd>
                    </div>
                  </div>
                  <div className="datagrid-item">
                    <div className="datagrid-title">{t("shortcutHelp")}</div>
                    <div className="datagrid-content">
                      <kbd>?</kbd>
                    </div>
                  </div>
                  <div className="datagrid-item">
                    <div className="datagrid-title">{t("shortcutClose")}</div>
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
