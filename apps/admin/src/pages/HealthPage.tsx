import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiClientError } from "@/api/client";
import { getHealthAggregate } from "@/api/ops";
import type { ServiceHealthItem } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { ErrorAlert, PageHeader } from "@/components/ui";

export function HealthPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.OpsHealthRead}>
      <HealthInner />
    </RequirePermission>
  );
}

function HealthInner() {
  const [items, setItems] = useState<ServiceHealthItem[]>([]);
  const [checkedAt, setCheckedAt] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await getHealthAggregate();
      setItems(data.services);
      setCheckedAt(data.checkedAtUtc);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : "Failed to load health.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <>
      <PageHeader
        pretitle="Observability"
        title="Service health"
        actions={
          <>
            <Link className="btn me-2" to="/platform">
              Services & packages
            </Link>
            <button type="button" className="btn" onClick={() => void load()} disabled={loading}>
              Refresh
            </button>
          </>
        }
      />
      <div className="page-body">
        <div className="container-xl">
          <ErrorAlert error={error} />
          {checkedAt ? (
            <div className="text-secondary mb-3">Checked at {new Date(checkedAt).toLocaleString()}</div>
          ) : null}
          <div className="row row-cards">
            {items.map((item) => (
              <div className="col-sm-6 col-lg-4" key={item.service}>
                <div className="card">
                  <div className="card-body">
                    <div className="subheader">{item.service}</div>
                    <div className="h3 mb-1">
                      <span
                        className={
                          item.status.toLowerCase() === "healthy"
                            ? "badge bg-green-lt"
                            : item.reachable
                              ? "badge bg-yellow-lt"
                              : "badge bg-red-lt"
                        }
                      >
                        {item.status}
                      </span>
                    </div>
                    <div className="text-secondary small">{item.description ?? (item.reachable ? "OK" : "Unreachable")}</div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </>
  );
}
