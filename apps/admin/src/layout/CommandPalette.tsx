import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { flattenNav, type NavEntry } from "./navConfig";

export function CommandPalette({
  open,
  onClose,
  can,
}: {
  open: boolean;
  onClose: () => void;
  can: (permission: string | string[]) => boolean;
}) {
  const { t } = useTranslation(["nav", "common"]);
  const navigate = useNavigate();
  const [query, setQuery] = useState("");

  const entries = useMemo(() => {
    const all = flattenNav().filter((item) => !item.permission || can(item.permission));
    const q = query.trim().toLowerCase();
    if (!q) return all;
    return all.filter((item) => {
      const label = t(item.labelKey).toLowerCase();
      const hay = `${label} ${item.to} ${(item.keywords ?? []).join(" ")}`.toLowerCase();
      return hay.includes(q);
    });
  }, [can, query, t]);

  useEffect(() => {
    if (!open) setQuery("");
  }, [open]);

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        if (open) onClose();
        else document.dispatchEvent(new CustomEvent("msf:open-command-palette"));
      }
      if (event.key === "Escape" && open) onClose();
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;

  function go(item: NavEntry) {
    navigate(item.to);
    onClose();
  }

  return (
    <div className="modal modal-blur fade show d-block" style={{ background: "rgba(0,0,0,.55)" }} role="dialog">
      <div className="modal-dialog modal-dialog-centered modal-lg" role="document">
        <div className="modal-content">
          <div className="modal-header py-2">
            <input
              autoFocus
              className="form-control form-control-lg border-0 shadow-none"
              placeholder={`${t("common:search")}… (Ctrl+K)`}
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter" && entries[0]) go(entries[0]);
              }}
            />
            <button type="button" className="btn-close" aria-label={t("common:close")} onClick={onClose} />
          </div>
          <div className="list-group list-group-flush" style={{ maxHeight: 360, overflow: "auto" }}>
            {entries.length === 0 ? (
              <div className="list-group-item text-secondary">{t("common:noResults")}</div>
            ) : (
              entries.map((item) => (
                <button
                  key={item.to}
                  type="button"
                  className="list-group-item list-group-item-action d-flex align-items-center gap-2"
                  onClick={() => go(item)}
                >
                  <span className="text-secondary">{item.icon}</span>
                  <span className="fw-medium">{t(item.labelKey)}</span>
                  <span className="ms-auto small text-secondary">{item.to}</span>
                </button>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
