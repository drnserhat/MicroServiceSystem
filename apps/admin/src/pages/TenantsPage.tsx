import { useEffect, useState, type FormEvent } from "react";
import { ApiClientError } from "@/api/client";
import { createTenant, listTenants, setTenantActive } from "@/api/identityAdmin";
import type { TenantItem } from "@/api/types";
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
  const toast = useToast();
  const { confirm } = useConfirm();
  const { can } = useAuth();
  const canWrite = can(FrameworkPermissions.IdentityTenantsWrite);
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
      setError(err instanceof ApiClientError ? err.message : "Failed to load tenants.");
      setItems([]);
    } finally {
      setLoading(false);
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
      toast.success(`Tenant "${name.trim()}" created.`);
      setName("");
      setSlug("");
      await load(page);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : "Create failed.";
      setError(msg);
      toast.error(msg);
    }
  }

  async function toggle(item: TenantItem) {
    if (!canWrite) return;
    const next = !item.isActive;
    const ok = await confirm({
      title: next ? "Activate tenant" : "Deactivate tenant",
      message: next
        ? `Activate tenant "${item.name}" (${item.slug})?`
        : `Deactivate tenant "${item.name}" (${item.slug})? Users in this tenant may lose access.`,
      confirmLabel: next ? "Activate" : "Deactivate",
      tone: next ? "primary" : "warning",
    });
    if (!ok) return;
    try {
      await setTenantActive(item.id, next);
      toast.success(`Tenant "${item.name}" ${next ? "activated" : "deactivated"}.`);
      await load(page);
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : "Update failed.";
      setError(msg);
      toast.error(msg);
    }
  }

  return (
    <PageFrame pretitle="Identity" title="Tenants"
    >
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
                  placeholder="Search name or slug"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
              <div className="col-md-4">
                <button type="submit" className="btn btn-primary w-100">
                  Search
                </button>
              </div>
            </div>
          </form>
          {canWrite ? (
            <form className="card mb-3" onSubmit={onCreate}>
              <div className="card-body row g-3">
                <div className="col-md-5">
                  <label className="form-label">Name</label>
                  <input className="form-control" value={name} onChange={(e) => setName(e.target.value)} required />
                </div>
                <div className="col-md-5">
                  <label className="form-label">Slug</label>
                  <input className="form-control" value={slug} onChange={(e) => setSlug(e.target.value)} required />
                </div>
                <div className="col-md-2 d-flex align-items-end">
                  <button type="submit" className="btn btn-primary w-100">
                    Create
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
                    <th>Name</th>
                    <th>Slug</th>
                    <th>Id</th>
                    <th>Status</th>
                    {canWrite ? <th /> : null}
                  </tr>
                </thead>
                <tbody>
                  {loading ? <TableSkeleton rows={5} cols={5} /> : null}
                  {items.map((item) => (
                    <tr key={item.id}>
                      <td>{item.name}</td>
                      <td>
                        <code>{item.slug}</code>
                      </td>
                      <td className="text-secondary">
                        <code>{item.id}</code>
                      </td>
                      <td>
                        <span className={item.isActive ? "badge bg-green-lt" : "badge bg-red-lt"}>
                          {item.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      {canWrite ? (
                        <td>
                          <button type="button" className="btn btn-sm" onClick={() => void toggle(item)}>
                            {item.isActive ? "Deactivate" : "Activate"}
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
