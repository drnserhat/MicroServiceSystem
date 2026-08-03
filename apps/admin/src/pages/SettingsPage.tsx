import { useEffect, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { ApiClientError, isServiceUnavailable } from "@/api/client";
import { deleteSetting, listSettings, upsertSetting } from "@/api/settings";
import type { SettingItem } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { PageFrame, DataTableShell, TableSkeleton } from "@/components/control";
import { useAuth } from "@/auth/AuthContext";
import { ErrorAlert, FieldErrors, PaginationBar, ServiceUnavailableAlert } from "@/components/ui";
import { useToast } from "@/ui/toast/ToastContext";
import { useConfirm } from "@/ui/dialog/ConfirmContext";

export function SettingsPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.SettingsValuesRead}>
      <SettingsPageInner />
    </RequirePermission>
  );
}

function SettingsPageInner() {
  const { t } = useTranslation(["settings", "common"]);
  const toast = useToast();
  const { confirm } = useConfirm();
  const { can } = useAuth();
  const canWrite = can(FrameworkPermissions.SettingsValuesWrite);
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<SettingItem[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [hasPrevious, setHasPrevious] = useState(false);
  const [hasNext, setHasNext] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [failures, setFailures] = useState<Record<string, string[]> | undefined>();
  const [unavailable, setUnavailable] = useState(false);
  const [loading, setLoading] = useState(true);
  const [key, setKey] = useState("");
  const [value, setValue] = useState("");
  const [editing, setEditing] = useState<SettingItem | null>(null);

  async function load(p = page) {
    setLoading(true);
    setError(null);
    setUnavailable(false);
    try {
      const data = await listSettings(p, 20);
      setItems([...data.items]);
      setTotalPages(data.totalPages || 1);
      setHasPrevious(data.hasPreviousPage);
      setHasNext(data.hasNextPage);
    } catch (err) {
      if (isServiceUnavailable(err)) {
        setUnavailable(true);
      } else {
        setError(err instanceof ApiClientError ? err.message : t("loadFailed"));
      }
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!canWrite) return;
    setError(null);
    setFailures(undefined);
    try {
      await upsertSetting(key.trim(), value, editing?.version ?? null);
      toast.success(
        editing ? t("updateSuccess", { key: key.trim() }) : t("saveSuccess", { key: key.trim() }),
      );
      setKey("");
      setValue("");
      setEditing(null);
      await load(page);
    } catch (err) {
      if (err instanceof ApiClientError) {
        setError(err.message);
        setFailures(err.failures);
        toast.error(err.message);
      } else {
        setError(t("common:saveFailed"));
        toast.error(t("common:saveFailed"));
      }
    }
  }

  async function onDelete(item: SettingItem) {
    if (!canWrite) return;
    const ok = await confirm({
      title: t("deleteTitle"),
      message: t("deleteMessage", { key: item.key }),
      confirmLabel: t("common:delete"),
      tone: "danger",
    });
    if (!ok) return;
    try {
      await deleteSetting(item.key, item.version);
      toast.success(t("deleteSuccess", { key: item.key }));
      await load(page);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("common:deleteFailed");
      setError(msg);
      toast.error(msg);
    }
  }

  function startEdit(item: SettingItem) {
    setEditing(item);
    setKey(item.key);
    setValue(item.value);
  }

  return (
    <PageFrame pretitle={t("pretitle")} title={t("title")}>
      {unavailable ? <ServiceUnavailableAlert service="Settings" /> : null}
      <ErrorAlert error={error} />
      <FieldErrors failures={failures} />

      {canWrite ? (
        <div className="card mb-3">
          <div className="card-header">
            <h3 className="card-title">{editing ? t("updateSetting") : t("createSetting")}</h3>
          </div>
          <form className="card-body" onSubmit={onSubmit}>
            <div className="row g-3">
              <div className="col-md-4">
                <label className="form-label">{t("colKey")}</label>
                <input
                  className="form-control"
                  value={key}
                  onChange={(e) => setKey(e.target.value)}
                  required
                  disabled={Boolean(editing)}
                />
              </div>
              <div className="col-md-6">
                <label className="form-label">{t("colValue")}</label>
                <input
                  className="form-control"
                  value={value}
                  onChange={(e) => setValue(e.target.value)}
                  required
                />
              </div>
              <div className="col-md-2 d-flex align-items-end gap-2">
                <button type="submit" className="btn btn-primary w-100">
                  {editing ? t("common:save") : t("common:create")}
                </button>
              </div>
            </div>
            {editing ? (
              <button
                type="button"
                className="btn btn-link px-0 mt-2"
                onClick={() => {
                  setEditing(null);
                  setKey("");
                  setValue("");
                }}
              >
                {t("cancelEdit")}
              </button>
            ) : null}
          </form>
        </div>
      ) : null}

      <DataTableShell
        title={t("tenantSettings")}
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
              <th>{t("colKey")}</th>
              <th>{t("colValue")}</th>
              <th className="w-1">{t("colVersion")}</th>
              {canWrite ? <th className="w-1" /> : null}
            </tr>
          </thead>
          <tbody>
            {loading ? <TableSkeleton rows={5} cols={4} /> : null}
            {!loading && items.length === 0 ? (
              <tr>
                <td colSpan={4} className="text-secondary">
                  {t("noSettingsYet")}
                </td>
              </tr>
            ) : null}
            {items.map((item) => (
              <tr key={item.id}>
                <td>
                  <code>{item.key}</code>
                </td>
                <td className="text-secondary">{item.value}</td>
                <td>
                  <span className="badge bg-azure-lt">{item.version}</span>
                </td>
                {canWrite ? (
                  <td className="text-nowrap">
                    <button type="button" className="btn btn-sm" onClick={() => startEdit(item)}>
                      {t("common:edit")}
                    </button>{" "}
                    <button type="button" className="btn btn-sm btn-danger" onClick={() => void onDelete(item)}>
                      {t("common:delete")}
                    </button>
                  </td>
                ) : null}
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
    </PageFrame>
  );
}
