import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { ApiClientError } from "@/api/client";
import { createRole, deleteRole, listRoles, replaceRole } from "@/api/identityAdmin";
import type { RoleItem } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions, KnownPermissionCodes } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { PageFrame, Skeleton } from "@/components/control";
import { ErrorAlert } from "@/components/ui";
import { useConfirm } from "@/ui/dialog/ConfirmContext";
import { useToast } from "@/ui/toast/ToastContext";

const BUILT_IN = new Set(["admin", "member"]);

function isBuiltInRole(name: string): boolean {
  return BUILT_IN.has(name.trim().toLowerCase());
}

export function RolesPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.IdentityRolesRead}>
      <RolesInner />
    </RequirePermission>
  );
}

function RolesInner() {
  const { t } = useTranslation(["roles", "common"]);
  const toast = useToast();
  const { confirm } = useConfirm();
  const { can } = useAuth();
  const canWrite = can(FrameworkPermissions.IdentityRolesWrite);

  const [items, setItems] = useState<RoleItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([]);
  const [editing, setEditing] = useState<RoleItem | null>(null);

  const permissionOptions = useMemo(
    () => [...KnownPermissionCodes].sort((a, b) => a.localeCompare(b)),
    [],
  );

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setItems(await listRoles());
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : t("loadFailed"));
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function resetForm() {
    setName("");
    setSelectedPermissions([]);
    setEditing(null);
  }

  function togglePermission(code: string) {
    setSelectedPermissions((current) =>
      current.includes(code) ? current.filter((item) => item !== code) : [...current, code],
    );
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!canWrite) return;
    setError(null);
    const trimmed = name.trim();
    try {
      if (editing) {
        await replaceRole(editing.id, { name: trimmed, permissions: selectedPermissions });
        toast.success(t("updateSuccess", { name: trimmed }));
      } else {
        await createRole({ name: trimmed, permissions: selectedPermissions });
        toast.success(t("createSuccess", { name: trimmed }));
      }
      resetForm();
      await load();
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("common:saveFailed");
      setError(msg);
      toast.error(msg);
    }
  }

  async function onDelete(role: RoleItem) {
    if (!canWrite || isBuiltInRole(role.name)) return;
    const ok = await confirm({
      title: t("deleteTitle"),
      message: t("deleteMessage", { name: role.name }),
      confirmLabel: t("common:delete"),
      tone: "danger",
    });
    if (!ok) return;
    try {
      await deleteRole(role.id);
      toast.success(t("deleteSuccess", { name: role.name }));
      if (editing?.id === role.id) resetForm();
      await load();
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("common:deleteFailed");
      setError(msg);
      toast.error(msg);
    }
  }

  function startEdit(role: RoleItem) {
    if (isBuiltInRole(role.name)) return;
    setEditing(role);
    setName(role.name);
    setSelectedPermissions([...role.permissions]);
  }

  return (
    <PageFrame pretitle={t("pretitle")} title={t("title")}>
      <p className="text-secondary small mb-3">{t("tokenNote")}</p>
      <ErrorAlert error={error} />

      {canWrite ? (
        <form className="card mb-3" onSubmit={onSubmit}>
          <div className="card-header">
            <h3 className="card-title">{editing ? t("editTitle") : t("createTitle")}</h3>
            {editing ? (
              <button type="button" className="btn btn-sm" onClick={resetForm}>
                {t("common:cancel")}
              </button>
            ) : null}
          </div>
          <div className="card-body">
            <div className="mb-3">
              <label className="form-label">{t("nameLabel")}</label>
              <input
                className="form-control"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                maxLength={128}
              />
            </div>
            <div className="mb-3">
              <div className="form-label">{t("permissionsLabel")}</div>
              <div className="row g-2" style={{ maxHeight: 220, overflow: "auto" }}>
                {permissionOptions.map((code) => (
                  <div className="col-md-6" key={code}>
                    <label className="form-check">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        checked={selectedPermissions.includes(code)}
                        onChange={() => togglePermission(code)}
                      />
                      <span className="form-check-label">
                        <code className="small">{code}</code>
                      </span>
                    </label>
                  </div>
                ))}
              </div>
            </div>
            <button type="submit" className="btn btn-primary">
              {editing ? t("common:save") : t("common:create")}
            </button>
          </div>
        </form>
      ) : null}

      {loading ? <Skeleton height={120} className="mb-3" /> : null}
      {!loading && items.length === 0 ? <p className="text-secondary">{t("empty")}</p> : null}

      <div className="row row-cards">
        {items.map((role) => {
          const builtIn = isBuiltInRole(role.name);
          return (
            <div className="col-md-6" key={role.id}>
              <div className="card">
                <div className="card-header">
                  <h3 className="card-title">
                    {role.name}
                    {builtIn ? (
                      <span className="badge bg-secondary-lt ms-2">{t("builtInBadge")}</span>
                    ) : null}
                  </h3>
                  {canWrite && !builtIn ? (
                    <div className="btn-list">
                      <button type="button" className="btn btn-sm" onClick={() => startEdit(role)}>
                        {t("common:edit")}
                      </button>
                      <button
                        type="button"
                        className="btn btn-sm btn-danger"
                        onClick={() => void onDelete(role)}
                      >
                        {t("common:delete")}
                      </button>
                    </div>
                  ) : null}
                </div>
                <div className="card-body">
                  <div className="form-label">{t("permissionsLabel")}</div>
                  <div className="d-flex flex-wrap gap-1">
                    {role.permissions.length === 0 ? (
                      <span className="text-secondary small">{t("noPermissions")}</span>
                    ) : (
                      role.permissions.map((permission) => (
                        <span key={permission} className="badge bg-azure-lt">
                          {permission}
                        </span>
                      ))
                    )}
                  </div>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </PageFrame>
  );
}
