import { Fragment, useEffect, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { ApiClientError } from "@/api/client";
import {
  createTenant,
  healthTenantDatabase,
  listTenantDatabases,
  listTenants,
  provisionTenantDatabase,
  retryTenantDatabase,
  setTenantActive,
} from "@/api/identityAdmin";
import type { TenantDatabaseBindingItem, TenantItem } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { PageFrame, TableSkeleton } from "@/components/control";
import { ErrorAlert, PaginationBar } from "@/components/ui";
import { useToast } from "@/ui/toast/ToastContext";
import { useConfirm } from "@/ui/dialog/ConfirmContext";

export function TenantsPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.IdentityTenantsRead}>
      <TenantsInner />
    </RequirePermission>
  );
}

function TenantsInner() {
  const { t } = useTranslation(["tenants", "common"]);
  const toast = useToast();
  const { confirm } = useConfirm();
  const { can } = useAuth();
  const canWrite = can(FrameworkPermissions.IdentityTenantsWrite);
  const canDbRead = can(FrameworkPermissions.IdentityTenantDatabasesRead);
  const canDbWrite = can(FrameworkPermissions.IdentityTenantDatabasesWrite);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [items, setItems] = useState<TenantItem[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [hasPrevious, setHasPrevious] = useState(false);
  const [hasNext, setHasNext] = useState(false);
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [bindings, setBindings] = useState<TenantDatabaseBindingItem[]>([]);
  const [bindingsLoading, setBindingsLoading] = useState(false);

  async function load(p = page) {
    setLoading(true);
    setError(null);
    try {
      const data = await listTenants(p, 20, search);
      setItems([...data.items]);
      setTotalPages(data.totalPages || 1);
      setHasPrevious(data.hasPreviousPage);
      setHasNext(data.hasNextPage);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : t("loadFailed"));
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadBindings(tenantId: string) {
    if (!canDbRead) return;
    setBindingsLoading(true);
    try {
      const rows = await listTenantDatabases(tenantId);
      setBindings(Array.isArray(rows) ? rows : []);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("dbLoadFailed");
      setError(msg);
      toast.error(msg);
      setBindings([]);
    } finally {
      setBindingsLoading(false);
    }
  }

  useEffect(() => {
    void load(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  async function onCreate(event: FormEvent) {
    event.preventDefault();
    if (!canWrite) return;
    try {
      await createTenant({ name: name.trim(), slug: slug.trim() });
      toast.success(t("createSuccess", { name: name.trim() }));
      setName("");
      setSlug("");
      await load(page);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("createFailed");
      setError(msg);
      toast.error(msg);
    }
  }

  async function toggle(item: TenantItem) {
    if (!canWrite) return;
    const next = !item.isActive;
    const ok = await confirm({
      title: next ? t("activateTitle") : t("deactivateTitle"),
      message: next
        ? t("activateMessage", { name: item.name, slug: item.slug })
        : t("deactivateMessage", { name: item.name, slug: item.slug }),
      confirmLabel: next ? t("activate") : t("deactivate"),
      tone: next ? "primary" : "warning",
    });
    if (!ok) return;
    try {
      await setTenantActive(item.id, next);
      toast.success(next ? t("activateSuccess", { name: item.name }) : t("deactivateSuccess", { name: item.name }));
      await load(page);
      if (expandedId === item.id) await loadBindings(item.id);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("updateFailed");
      setError(msg);
      toast.error(msg);
    }
  }

  async function toggleExpand(item: TenantItem) {
    if (!canDbRead) return;
    if (expandedId === item.id) {
      setExpandedId(null);
      setBindings([]);
      return;
    }
    setExpandedId(item.id);
    await loadBindings(item.id);
  }

  async function onProvision(tenantId: string) {
    if (!canDbWrite) return;
    try {
      const result = await provisionTenantDatabase(tenantId, "user");
      toast.success(t("dbProvisionDone", { status: result.status, db: result.databaseName }));
      await loadBindings(tenantId);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("dbActionFailed");
      toast.error(msg);
    }
  }

  async function onRetry(tenantId: string, serviceKey: string) {
    if (!canDbWrite) return;
    try {
      const result = await retryTenantDatabase(tenantId, serviceKey);
      toast.success(t("dbRetryDone", { status: result.status }));
      await loadBindings(tenantId);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("dbActionFailed");
      toast.error(msg);
    }
  }

  async function onHealth(tenantId: string, serviceKey: string) {
    if (!canDbWrite) return;
    try {
      const result = await healthTenantDatabase(tenantId, serviceKey);
      if (result.healthy) toast.success(t("dbHealthOk", { status: result.status }));
      else toast.error(t("dbHealthFail", { detail: result.detail ?? result.status }));
      await loadBindings(tenantId);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : t("dbActionFailed");
      toast.error(msg);
    }
  }

  return (
    <PageFrame pretitle={t("pretitle")} title={t("title")}>
      <ErrorAlert error={error} />
      <form
        className="card mb-3"
        onSubmit={(e) => {
          e.preventDefault();
          if (page === 1) void load(1);
          else setPage(1);
        }}
      >
        <div className="card-body row g-2">
          <div className="col-md-8">
            <input
              className="form-control"
              placeholder={t("searchPlaceholder")}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="col-md-4">
            <button type="submit" className="btn btn-primary w-100">
              {t("common:search")}
            </button>
          </div>
        </div>
      </form>
      {canWrite ? (
        <form className="card mb-3" onSubmit={onCreate}>
          <div className="card-body row g-3">
            <div className="col-md-5">
              <label className="form-label">{t("colName")}</label>
              <input className="form-control" value={name} onChange={(e) => setName(e.target.value)} required />
            </div>
            <div className="col-md-5">
              <label className="form-label">{t("colSlug")}</label>
              <input className="form-control" value={slug} onChange={(e) => setSlug(e.target.value)} required />
            </div>
            <div className="col-md-2 d-flex align-items-end">
              <button type="submit" className="btn btn-primary w-100">
                {t("common:create")}
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
                <th>{t("colName")}</th>
                <th>{t("colSlug")}</th>
                <th>{t("colId")}</th>
                <th>{t("colStatus")}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {loading ? <TableSkeleton rows={5} cols={5} /> : null}
              {items.map((item) => (
                <Fragment key={item.id}>
                  <tr>
                    <td>{item.name}</td>
                    <td>
                      <code>{item.slug}</code>
                    </td>
                    <td className="text-secondary">
                      <code>{item.id}</code>
                    </td>
                    <td>
                      <span className={item.isActive ? "badge bg-green-lt" : "badge bg-red-lt"}>
                        {item.isActive ? t("common:active") : t("common:inactive")}
                      </span>
                    </td>
                    <td className="text-end text-nowrap">
                      {canDbRead ? (
                        <button
                          type="button"
                          className="btn btn-sm me-1"
                          onClick={() => void toggleExpand(item)}
                        >
                          {expandedId === item.id ? t("hideDatabases") : t("showDatabases")}
                        </button>
                      ) : null}
                      {canWrite ? (
                        <button type="button" className="btn btn-sm" onClick={() => void toggle(item)}>
                          {item.isActive ? t("deactivate") : t("activate")}
                        </button>
                      ) : null}
                    </td>
                  </tr>
                  {expandedId === item.id ? (
                    <tr>
                      <td colSpan={5}>
                        <div className="p-2">
                          <div className="d-flex justify-content-between align-items-center mb-2">
                            <strong>{t("databasesTitle")}</strong>
                            {canDbWrite ? (
                              <button
                                type="button"
                                className="btn btn-sm btn-primary"
                                onClick={() => void onProvision(item.id)}
                              >
                                {t("provisionUserDb")}
                              </button>
                            ) : null}
                          </div>
                          {bindingsLoading ? <TableSkeleton rows={2} cols={4} /> : null}
                          {!bindingsLoading && bindings.length === 0 ? (
                            <p className="text-secondary mb-0">{t("noDatabases")}</p>
                          ) : null}
                          {!bindingsLoading && bindings.length > 0 ? (
                            <table className="table table-sm mb-0">
                              <thead>
                                <tr>
                                  <th>{t("colService")}</th>
                                  <th>{t("colCluster")}</th>
                                  <th>{t("colDatabase")}</th>
                                  <th>{t("colDbStatus")}</th>
                                  <th>{t("colSchema")}</th>
                                  {canDbWrite ? <th /> : null}
                                </tr>
                              </thead>
                              <tbody>
                                {bindings.map((b) => (
                                  <tr key={b.id}>
                                    <td>
                                      <code>{b.serviceKey}</code>
                                    </td>
                                    <td>
                                      <code>{b.clusterSlug}</code>
                                    </td>
                                    <td>
                                      <code>{b.databaseName}</code>
                                    </td>
                                    <td>
                                      <span className="badge bg-azure-lt">{b.status}</span>
                                      {b.lastError ? (
                                        <div className="small text-danger mt-1">{b.lastError}</div>
                                      ) : null}
                                    </td>
                                    <td>{b.schemaVersion ?? "—"}</td>
                                    {canDbWrite ? (
                                      <td className="text-nowrap">
                                        <button
                                          type="button"
                                          className="btn btn-sm me-1"
                                          onClick={() => void onRetry(item.id, b.serviceKey)}
                                        >
                                          {t("retry")}
                                        </button>
                                        <button
                                          type="button"
                                          className="btn btn-sm"
                                          onClick={() => void onHealth(item.id, b.serviceKey)}
                                        >
                                          {t("health")}
                                        </button>
                                      </td>
                                    ) : null}
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          ) : null}
                        </div>
                      </td>
                    </tr>
                  ) : null}
                </Fragment>
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
