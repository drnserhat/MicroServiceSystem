import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ApiClientError } from "@/api/client";
import { getHealthAggregate } from "@/api/ops";
import type { ServiceHealthItem } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import {
  EmptyState,
  HealthIndicator,
  PreviewBanner,
  SectionHeader,
  ServiceCard,
  Skeleton,
  StatusBadge,
} from "@/components/control";
import { PLATFORM_PACKAGES, type PlatformPackage } from "@/platform/catalog";

function healthFor(pkg: PlatformPackage, items: ServiceHealthItem[]) {
  if (!pkg.healthService) return undefined;
  return items.find((i) => i.service === pkg.healthService);
}

export function ServicesPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.OpsHealthRead}>
      <ServicesInner />
    </RequirePermission>
  );
}

function ServicesInner() {
  const { serviceId } = useParams<{ serviceId?: string }>();
  const navigate = useNavigate();
  const [health, setHealth] = useState<ServiceHealthItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const services = useMemo(
    () => PLATFORM_PACKAGES.filter((p) => p.healthService || ["gateway", "admin"].includes(p.id)),
    [],
  );

  useEffect(() => {
    let cancelled = false;
    void getHealthAggregate()
      .then((data) => {
        if (!cancelled) setHealth(data.services);
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof ApiClientError ? err.message : "Health load failed.");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const selected = serviceId ? PLATFORM_PACKAGES.find((p) => p.id === serviceId) : undefined;
  const selectedHealth = selected ? healthFor(selected, health) : undefined;

  return (
    <>
      <SectionHeader
        title="Service Center"
        description="Microservice inventory with live readiness where gateway clusters expose probes."
        actions={
          <Link className="btn" to="/platform">
            Packages
          </Link>
        }
      />
      {error ? <div className="alert alert-danger">{error}</div> : null}
      <PreviewBanner>
        CPU, memory, secrets, restart, and health history controls are UI placeholders — no backend ops API.
      </PreviewBanner>

      {loading ? (
        <div className="row row-cards">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div className="col-md-4" key={i}>
              <Skeleton height={160} />
            </div>
          ))}
        </div>
      ) : (
        <div className="row row-cards">
          {services.map((pkg) => {
            const live = healthFor(pkg, health);
            return (
              <div className="col-md-4" key={pkg.id}>
                <div
                  role="button"
                  tabIndex={0}
                  onClick={() => navigate(`/services/${pkg.id}`)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") navigate(`/services/${pkg.id}`);
                  }}
                >
                  <ServiceCard
                    name={pkg.name}
                    summary={pkg.summary}
                    status={live?.status ?? (pkg.kind === "core" ? "Infra" : "Optional")}
                    reachable={live?.reachable}
                    kind={pkg.kind}
                    actions={
                      <div className="btn-list">
                        {pkg.adminPath ? (
                          <Link
                            className="btn btn-sm"
                            to={pkg.adminPath}
                            onClick={(e) => e.stopPropagation()}
                          >
                            Open
                          </Link>
                        ) : null}
                        {pkg.gatewayPrefix && pkg.gatewayPrefix !== "/" ? (
                          <a
                            className="btn btn-sm"
                            href={`/docs/${pkg.id}/swagger.json`}
                            target="_blank"
                            rel="noreferrer"
                            onClick={(e) => e.stopPropagation()}
                          >
                            OpenAPI
                          </a>
                        ) : null}
                      </div>
                    }
                  />
                </div>
              </div>
            );
          })}
        </div>
      )}

      {selected ? (
        <div className="card mt-3">
          <div className="card-header">
            <h3 className="card-title d-flex align-items-center gap-2">
              <HealthIndicator status={selectedHealth?.status} reachable={selectedHealth?.reachable} />
              {selected.name}
            </h3>
            <div className="card-actions">
              <StatusBadge status={selectedHealth?.status ?? selected.kind} />
            </div>
          </div>
          <div className="card-body">
            <p className="text-secondary">{selected.summary}</p>
            <div className="datagrid">
              <div className="datagrid-item">
                <div className="datagrid-title">Compose</div>
                <div className="datagrid-content">{selected.composeNote}</div>
              </div>
              <div className="datagrid-item">
                <div className="datagrid-title">Gateway prefix</div>
                <div className="datagrid-content">
                  <code>{selected.gatewayPrefix ?? "—"}</code>
                </div>
              </div>
              <div className="datagrid-item">
                <div className="datagrid-title">Dependencies</div>
                <div className="datagrid-content text-secondary">
                  Preview: PostgreSQL / Redis / RabbitMQ (service-specific wiring not exposed via API)
                </div>
              </div>
              <div className="datagrid-item">
                <div className="datagrid-title">CPU / Memory</div>
                <div className="datagrid-content text-secondary">Preview — awaiting metrics API</div>
              </div>
              <div className="datagrid-item">
                <div className="datagrid-title">Observability</div>
                <div className="datagrid-content btn-list">
                  <a className="btn btn-sm" href="http://localhost:5341" target="_blank" rel="noreferrer">
                    Seq
                  </a>
                  <a className="btn btn-sm" href="http://localhost:16686" target="_blank" rel="noreferrer">
                    Jaeger
                  </a>
                  <a className="btn btn-sm" href="http://localhost:3000" target="_blank" rel="noreferrer">
                    Grafana
                  </a>
                </div>
              </div>
              <div className="datagrid-item">
                <div className="datagrid-title">Actions</div>
                <div className="datagrid-content">
                  <button type="button" className="btn btn-sm" disabled title="No ops API">
                    Restart
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      ) : (
        <div className="mt-3">
          <EmptyState title="Select a service card" description="Detail drawer fields appear below the grid." />
        </div>
      )}
    </>
  );
}
