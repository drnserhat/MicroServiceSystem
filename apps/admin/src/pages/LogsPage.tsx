import { useEffect, useState, type FormEvent } from "react";
import { useSearchParams } from "react-router-dom";
import { ApiClientError, isServiceUnavailable } from "@/api/client";
import { listLogs } from "@/api/logging";
import type { SystemLog } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { LogViewer, PageFrame, TableSkeleton } from "@/components/control";
import { ErrorAlert, PaginationBar, ServiceUnavailableAlert } from "@/components/ui";

export function LogsPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.LoggingLogsRead}>
      <PageFrame pretitle="Observability" title="System logs">
        <LogsBrowser />
      </PageFrame>
    </RequirePermission>
  );
}

/** Shared browser for standalone `/logs` and Observability hub embed */
export function LogsBrowser() {
  const [params, setParams] = useSearchParams();
  const [page, setPage] = useState(1);
  const [level, setLevel] = useState("");
  const [source, setSource] = useState("");
  const [correlationId, setCorrelationId] = useState(() => params.get("correlationId") ?? "");
  const [items, setItems] = useState<SystemLog[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [hasPrevious, setHasPrevious] = useState(false);
  const [hasNext, setHasNext] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [unavailable, setUnavailable] = useState(false);

  async function load(p = page, corr = correlationId) {
    setLoading(true);
    setError(null);
    setUnavailable(false);
    try {
      const data = await listLogs({
        pageNumber: p,
        pageSize: 20,
        level: level || undefined,
        source: source || undefined,
        correlationId: corr || undefined,
      });
      setItems([...data.items]);
      setTotalPages(data.totalPages || 1);
      setHasPrevious(data.hasPreviousPage);
      setHasNext(data.hasNextPage);
    } catch (err) {
      if (isServiceUnavailable(err)) setUnavailable(true);
      else setError(err instanceof ApiClientError ? err.message : "Failed to load logs.");
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const fromUrl = params.get("correlationId") ?? "";
    if (fromUrl !== correlationId) {
      setCorrelationId(fromUrl);
      setPage(1);
      void load(1, fromUrl);
      return;
    }
    void load(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, params]);

  function onFilter(event: FormEvent) {
    event.preventDefault();
    const next = new URLSearchParams(params);
    if (correlationId.trim()) next.set("correlationId", correlationId.trim());
    else next.delete("correlationId");
    setParams(next, { replace: true });
    if (page === 1) void load(1, correlationId.trim());
    else setPage(1);
  }

  return (
    <>
      {unavailable ? <ServiceUnavailableAlert service="Logging" /> : null}
      <ErrorAlert error={error} />
      <form className="card mb-3" onSubmit={onFilter}>
        <div className="card-body row g-2">
          <div className="col-md-3">
            <input
              className="form-control"
              placeholder="Level"
              value={level}
              onChange={(e) => setLevel(e.target.value)}
            />
          </div>
          <div className="col-md-3">
            <input
              className="form-control"
              placeholder="Source"
              value={source}
              onChange={(e) => setSource(e.target.value)}
            />
          </div>
          <div className="col-md-4">
            <input
              className="form-control"
              placeholder="Correlation id"
              value={correlationId}
              onChange={(e) => setCorrelationId(e.target.value)}
            />
          </div>
          <div className="col-md-2">
            <button type="submit" className="btn btn-primary w-100">
              Filter
            </button>
          </div>
        </div>
      </form>
      <LogViewer
        loading={loading}
        empty={!loading && items.length === 0}
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
              <th>Time</th>
              <th>Level</th>
              <th>Source</th>
              <th>Message</th>
            </tr>
          </thead>
          <tbody>
            {loading ? <TableSkeleton rows={5} cols={4} /> : null}
            {!loading && items.length === 0 ? (
              <tr>
                <td colSpan={4} className="text-secondary">
                  No logs.
                </td>
              </tr>
            ) : null}
            {items.map((item) => (
              <tr key={item.id}>
                <td className="text-secondary text-nowrap">{new Date(item.timestamp).toLocaleString()}</td>
                <td>
                  <span className="badge">{item.level}</span>
                </td>
                <td>{item.source ?? "—"}</td>
                <td>{item.message}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </LogViewer>
    </>
  );
}
