import { useEffect, useState, type FormEvent } from "react";
import { ApiClientError, isServiceUnavailable } from "@/api/client";
import { listLogs } from "@/api/logging";
import type { SystemLog } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { ErrorAlert, PageHeader, PaginationBar, ServiceUnavailableAlert } from "@/components/ui";

export function LogsPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.LoggingLogsRead}>
      <LogsInner />
    </RequirePermission>
  );
}

function LogsInner() {
  const [page, setPage] = useState(1);
  const [level, setLevel] = useState("");
  const [source, setSource] = useState("");
  const [correlationId, setCorrelationId] = useState("");
  const [items, setItems] = useState<SystemLog[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [hasPrevious, setHasPrevious] = useState(false);
  const [hasNext, setHasNext] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [unavailable, setUnavailable] = useState(false);

  async function load(p = page) {
    setLoading(true);
    setError(null);
    setUnavailable(false);
    try {
      const data = await listLogs({
        pageNumber: p,
        pageSize: 20,
        level: level || undefined,
        source: source || undefined,
        correlationId: correlationId || undefined,
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
    void load(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  function onFilter(event: FormEvent) {
    event.preventDefault();
    if (page === 1) void load(1);
    else setPage(1);
  }

  return (
    <>
      <PageHeader pretitle="Observability" title="System logs" />
      <div className="page-body">
        <div className="container-xl">
          {unavailable ? <ServiceUnavailableAlert service="Logging" /> : null}
          <ErrorAlert error={error} />
          <form className="card mb-3" onSubmit={onFilter}>
            <div className="card-body row g-2">
              <div className="col-md-3">
                <input className="form-control" placeholder="Level" value={level} onChange={(e) => setLevel(e.target.value)} />
              </div>
              <div className="col-md-3">
                <input className="form-control" placeholder="Source" value={source} onChange={(e) => setSource(e.target.value)} />
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
          <div className="card">
            <div className="table-responsive">
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
                  {loading ? (
                    <tr>
                      <td colSpan={4} className="text-secondary">
                        Loading…
                      </td>
                    </tr>
                  ) : null}
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
        </div>
      </div>
    </>
  );
}
