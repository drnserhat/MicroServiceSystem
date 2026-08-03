import { useEffect, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { ApiClientError, isServiceUnavailable } from "@/api/client";
import { createCountry, deleteCountry, listCountries, updateCountry } from "@/api/location";
import type { Country } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { PageFrame, TableSkeleton } from "@/components/control";
import { ErrorAlert, PaginationBar, ServiceUnavailableAlert } from "@/components/ui";
import { useToast } from "@/ui/toast/ToastContext";
import { useConfirm } from "@/ui/dialog/ConfirmContext";

export function CountriesPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.LocationCountriesRead}>
      <CountriesInner />
    </RequirePermission>
  );
}

function CountriesInner() {
  const { t } = useTranslation(["countries", "common"]);
  const toast = useToast();
  const { confirm } = useConfirm();
  const { can } = useAuth();
  const canCreate = can(FrameworkPermissions.LocationCountriesCreate);
  const canWrite = can(FrameworkPermissions.LocationCountriesWrite);
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<Country[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [hasPrevious, setHasPrevious] = useState(false);
  const [hasNext, setHasNext] = useState(false);
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [editing, setEditing] = useState<Country | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [unavailable, setUnavailable] = useState(false);

  async function load(p = page) {
    setLoading(true);
    setError(null);
    setUnavailable(false);
    try {
      const data = await listCountries(p, 20);
      setItems([...data.items]);
      setTotalPages(data.totalPages || 1);
      setHasPrevious(data.hasPreviousPage);
      setHasNext(data.hasNextPage);
    } catch (err) {
      if (isServiceUnavailable(err)) setUnavailable(true);
      else setError(err instanceof ApiClientError ? err.message : t("loadFailed"));
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
    setError(null);
    try {
      if (editing) {
        await updateCountry(editing.code, name.trim(), editing.version);
        toast.success(t("updateSuccess", { code: editing.code }));
      } else {
        const nextCode = code.trim().toUpperCase();
        await createCountry(nextCode, name.trim());
        toast.success(t("createSuccess", { code: nextCode }));
      }
      setCode("");
      setName("");
      setEditing(null);
      await load(page);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("common:saveFailed");
      setError(msg);
      toast.error(msg);
    }
  }

  async function onDelete(item: Country) {
    if (!canWrite) return;
    const ok = await confirm({
      title: t("deleteTitle"),
      message: t("deleteMessage", { code: item.code, name: item.name }),
      confirmLabel: t("common:delete"),
      tone: "danger",
    });
    if (!ok) return;
    try {
      await deleteCountry(item.code, item.version);
      toast.success(t("deleteSuccess", { code: item.code }));
      await load(page);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("common:deleteFailed");
      setError(msg);
      toast.error(msg);
    }
  }

  return (
    <PageFrame pretitle={t("pretitle")} title={t("title")}>
      {unavailable ? <ServiceUnavailableAlert service="Location" /> : null}
      <ErrorAlert error={error} />
      {(canCreate || editing) && (canCreate || canWrite) ? (
        <form className="card mb-3" onSubmit={onSubmit}>
          <div className="card-body row g-3">
            <div className="col-md-3">
              <label className="form-label">{t("colCode")}</label>
              <input
                className="form-control"
                value={editing ? editing.code : code}
                onChange={(e) => setCode(e.target.value)}
                disabled={Boolean(editing)}
                required={!editing}
              />
            </div>
            <div className="col-md-6">
              <label className="form-label">{t("colName")}</label>
              <input className="form-control" value={name} onChange={(e) => setName(e.target.value)} required />
            </div>
            <div className="col-md-3 d-flex align-items-end">
              <button type="submit" className="btn btn-primary w-100">
                {editing ? t("common:save") : t("common:create")}
              </button>
            </div>
          </div>
        </form>
      ) : null}
      <div className="card">
        <div className="table-responsive">
          <table className="table table-vcenter card-table">
            <thead>
              <tr>
                <th>{t("colCode")}</th>
                <th>{t("colName")}</th>
                <th>{t("common:version")}</th>
                {canWrite ? <th /> : null}
              </tr>
            </thead>
            <tbody>
              {loading ? <TableSkeleton rows={5} cols={4} /> : null}
              {!loading && items.length === 0 ? (
                <tr>
                  <td colSpan={canWrite ? 4 : 3} className="text-secondary">
                    {t("emptyHint")}
                  </td>
                </tr>
              ) : null}
              {items.map((item) => (
                <tr key={item.id}>
                  <td>
                    <code>{item.code}</code>
                  </td>
                  <td>{item.name}</td>
                  <td>{item.version}</td>
                  {canWrite ? (
                    <td className="text-nowrap">
                      <button
                        type="button"
                        className="btn btn-sm"
                        onClick={() => {
                          setEditing(item);
                          setName(item.name);
                        }}
                      >
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
        </div>
        <PaginationBar
          page={page}
          totalPages={totalPages}
          hasPrevious={hasPrevious}
          hasNext={hasNext}
          loading={loading}
          onChange={setPage}
        />
      </div>
    </PageFrame>
  );
}
