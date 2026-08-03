import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ApiClientError } from "@/api/client";
import {
  assignUserRole,
  disableIdentityUser,
  listIdentityUsers,
  listRoles,
  unassignUserRole,
} from "@/api/identityAdmin";
import type { IdentityUserItem, RoleItem } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { DataTableShell, FilterBar, PageFrame, TableSkeleton } from "@/components/control";
import { ErrorAlert, PaginationBar } from "@/components/ui";
import { useConfirm } from "@/ui/dialog/ConfirmContext";
import { useToast } from "@/ui/toast/ToastContext";

export function UsersDirectoryPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.IdentityUsersRead}>
      <UsersInner />
    </RequirePermission>
  );
}

function UsersInner() {
  const { t } = useTranslation(["users", "common"]);
  const toast = useToast();
  const { confirm, prompt } = useConfirm();
  const { can } = useAuth();
  const canDisable = can(FrameworkPermissions.IdentityUsersDisable);
  const canAssign = can(FrameworkPermissions.IdentityRolesAssign);
  const canReadRoles = can(FrameworkPermissions.IdentityRolesRead);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [items, setItems] = useState<IdentityUserItem[]>([]);
  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [hasPrevious, setHasPrevious] = useState(false);
  const [hasNext, setHasNext] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [assignDraft, setAssignDraft] = useState<Record<string, string>>({});
  const [busyKey, setBusyKey] = useState<string | null>(null);

  const rolesById = useMemo(() => new Map(roles.map((role) => [role.id, role])), [roles]);

  async function load(p = page) {
    setLoading(true);
    setError(null);
    try {
      const [usersData, rolesData] = await Promise.all([
        listIdentityUsers(p, 20, search),
        canReadRoles || canAssign ? listRoles() : Promise.resolve([] as RoleItem[]),
      ]);
      setItems([...usersData.items]);
      setRoles(rolesData);
      setTotalPages(usersData.totalPages || 1);
      setHasPrevious(usersData.hasPreviousPage);
      setHasNext(usersData.hasNextPage);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : t("loadFailed"));
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  async function onDisable(item: IdentityUserItem) {
    if (!canDisable) return;
    const reason = await prompt({
      title: t("disableTitle"),
      message: t("disableMessage", { email: item.email }),
      promptLabel: t("disableReasonLabel"),
      defaultValue: t("disableReasonDefault"),
      confirmLabel: t("disable"),
      tone: "danger",
      required: true,
    });
    if (reason === null) return;
    try {
      await disableIdentityUser(item.id, reason.trim() || t("disableReasonDefault"));
      toast.success(t("disableSuccess", { email: item.email }));
      await load(page);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("disableFailed");
      setError(msg);
      toast.error(msg);
    }
  }

  async function onAssign(item: IdentityUserItem) {
    if (!canAssign) return;
    const roleId = assignDraft[item.id];
    if (!roleId) return;
    const role = rolesById.get(roleId);
    setBusyKey(`${item.id}:assign`);
    try {
      await assignUserRole(item.id, roleId);
      toast.success(t("assignSuccess", { role: role?.name ?? "role", email: item.email }));
      setAssignDraft((prev) => ({ ...prev, [item.id]: "" }));
      await load(page);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("assignFailed");
      setError(msg);
      toast.error(msg);
    } finally {
      setBusyKey(null);
    }
  }

  async function onUnassign(item: IdentityUserItem, role: RoleItem) {
    if (!canAssign) return;
    const ok = await confirm({
      title: t("removeRoleTitle"),
      message: t("removeRoleMessage", { role: role.name, email: item.email }),
      confirmLabel: t("common:remove"),
      tone: "warning",
    });
    if (!ok) return;
    setBusyKey(`${item.id}:${role.id}`);
    try {
      await unassignUserRole(item.id, role.id);
      toast.success(t("removeRoleSuccess", { role: role.name, email: item.email }));
      await load(page);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("unassignFailed");
      setError(msg);
      toast.error(msg);
    } finally {
      setBusyKey(null);
    }
  }

  const colCount = 5;

  return (
    <PageFrame
      pretitle={t("pretitle")}
      title={t("title")}
      actions={
        can(FrameworkPermissions.RegistrationUsersCreate) ? (
          <Link className="btn btn-primary" to="/users/register">
            {t("registerUser")}
          </Link>
        ) : null
      }
    >
      <ErrorAlert error={error} />
      <FilterBar
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={t("searchPlaceholder")}
        trailing={
          <button
            type="button"
            className="btn btn-primary w-100"
            onClick={() => {
              if (page === 1) void load(1);
              else setPage(1);
            }}
          >
            {t("common:search")}
          </button>
        }
      />

      <DataTableShell
        title={t("directory")}
        footer={
          <PaginationBar
            page={page}
            totalPages={totalPages}
            hasPrevious={hasPrevious}
            hasNext={hasNext}
            loading={loading}
            onChange={setPage}
          />
        }
      >
        <table className="table table-vcenter card-table">
          <thead>
            <tr>
              <th>{t("colEmail")}</th>
              <th>{t("colUsername")}</th>
              <th>{t("colRoles")}</th>
              <th>{t("colStatus")}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {loading ? <TableSkeleton rows={5} cols={colCount} /> : null}
            {!loading && items.length === 0 ? (
              <tr>
                <td colSpan={colCount} className="text-secondary">
                  {t("empty")}
                </td>
              </tr>
            ) : null}
            {items.map((item) => {
              const assigned = item.roleIds
                .map((id) => rolesById.get(id))
                .filter((role): role is RoleItem => Boolean(role));
              const available = roles.filter((role) => !item.roleIds.includes(role.id));
              return (
                <tr key={item.id}>
                  <td>{item.email}</td>
                  <td>{item.userName}</td>
                  <td style={{ minWidth: 220 }}>
                    <div className="d-flex flex-wrap gap-1 mb-1">
                      {assigned.length === 0 ? (
                        <span className="text-secondary">
                          {item.roleIds.length > 0 && !canReadRoles && !canAssign
                            ? t("rolesCount", { count: item.roleIds.length })
                            : t("rolesNone")}
                        </span>
                      ) : (
                        assigned.map((role) => (
                          <span key={role.id} className="badge bg-azure-lt d-inline-flex align-items-center gap-1">
                            {role.name}
                            {canAssign ? (
                              <button
                                type="button"
                                className="btn-close btn-close-sm"
                                aria-label={t("removeRoleAria", { role: role.name })}
                                disabled={busyKey === `${item.id}:${role.id}`}
                                onClick={() => void onUnassign(item, role)}
                              />
                            ) : null}
                          </span>
                        ))
                      )}
                    </div>
                    {canAssign && available.length > 0 ? (
                      <div className="input-group input-group-sm" style={{ maxWidth: 280 }}>
                        <select
                          className="form-select"
                          value={assignDraft[item.id] ?? ""}
                          onChange={(e) =>
                            setAssignDraft((prev) => ({ ...prev, [item.id]: e.target.value }))
                          }
                        >
                          <option value="">{t("assignRole")}</option>
                          {available.map((role) => (
                            <option key={role.id} value={role.id}>
                              {role.name}
                            </option>
                          ))}
                        </select>
                        <button
                          type="button"
                          className="btn"
                          disabled={!assignDraft[item.id] || busyKey === `${item.id}:assign`}
                          onClick={() => void onAssign(item)}
                        >
                          {t("common:add")}
                        </button>
                      </div>
                    ) : null}
                  </td>
                  <td>
                    <span className={item.isActive ? "badge bg-green-lt" : "badge bg-red-lt"}>
                      {item.isActive ? t("common:active") : t("common:disabled")}
                    </span>
                  </td>
                  <td className="text-nowrap">
                    <Link className="btn btn-sm" to={`/users/${item.id}`}>
                      {t("openProfile")}
                    </Link>{" "}
                    {canDisable && item.isActive ? (
                      <button type="button" className="btn btn-sm btn-danger" onClick={() => void onDisable(item)}>
                        {t("disable")}
                      </button>
                    ) : null}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </DataTableShell>
    </PageFrame>
  );
}
