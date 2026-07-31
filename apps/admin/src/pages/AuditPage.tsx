import { useEffect, useState } from "react";
import { ApiClientError, isServiceUnavailable } from "@/api/client";
import { listAuditEntries } from "@/api/audit";
import type { AuditEntry } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { PageFrame, DataTableShell, TableSkeleton } from "@/components/control";
import { ErrorAlert, PaginationBar, ServiceUnavailableAlert } from "@/components/ui";

export function AuditPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.AuditEntriesRead}>
      <PageFrame pretitle="Observability" title="Audit">
        <AuditBrowser />
      </PageFrame>
    </RequirePermission>
  );
}

/** Shared browser for standalone `/audit` and Observability hub embed */
export function AuditBrowser() {
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<AuditEntry[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [hasPrevious, setHasPrevious] = useState(false);
  const [hasNext, setHasNext] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [unavailable, setUnavailable] = useState(false);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setLoading(true);
      setError(null);
      setUnavailable(false);
      try {
        const data = await listAuditEntries(page, 20);
        if (cancelled) return;
        setItems([...data.items]);
        setTotalPages(data.totalPages || 1);
        setHasPrevious(data.hasPreviousPage);
        setHasNext(data.hasNextPage);
      } catch (err) {
        if (cancelled) return;
        if (isServiceUnavailable(err)) setUnavailable(true);
        else setError(err instanceof ApiClientError ? err.message : "Failed to load audit.");
        setItems([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, [page]);

  return (
    <>
      {unavailable ? <ServiceUnavailableAlert service="Audit" /> : null}
      <ErrorAlert error={error} />
      <DataTableShell
        title="Audit entries"
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
              <th>Action</th>
              <th>Resource</th>
              <th>Actor</th>
              <th>Details</th>
            </tr>
          </thead>
          <tbody>
            {loading ? <TableSkeleton rows={5} cols={4} /> : null}
            {!loading && items.length === 0 ? (
              <tr>
                <td colSpan={4} className="text-secondary">
                  No audit entries.
                </td>
              </tr>
            ) : null}
            {items.map((item) => (
              <tr key={item.id}>
                <td>{item.action}</td>
                <td>
                  <code>
                    {item.resourceType}/{item.resourceId}
                  </code>
                </td>
                <td className="text-secondary">{item.actorUserId ?? "—"}</td>
                <td className="text-secondary">{item.details ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </DataTableShell>
    </>
  );
}
