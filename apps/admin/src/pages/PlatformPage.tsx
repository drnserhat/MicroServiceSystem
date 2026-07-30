import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ApiClientError } from "@/api/client";
import { getHealthAggregate } from "@/api/ops";
import type { ServiceHealthItem } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { ErrorAlert, PageHeader } from "@/components/ui";
import { PLATFORM_PACKAGES, type PlatformPackage, type PlatformPackageKind } from "@/platform/catalog";

export function PlatformPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.OpsHealthRead}>
      <PlatformInner />
    </RequirePermission>
  );
}

function kindLabel(kind: PlatformPackageKind): string {
  switch (kind) {
    case "core":
      return "Core (lite)";
    case "addon":
      return "Add-on (full)";
    case "observability":
      return "Observability";
  }
}

function statusFor(pkg: PlatformPackage, health: ServiceHealthItem[]): ServiceHealthItem | undefined {
  if (!pkg.healthService) return undefined;
  return health.find((item) => item.service === pkg.healthService);
}

function PlatformInner() {
  const [health, setHealth] = useState<ServiceHealthItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<"all" | PlatformPackageKind>("all");

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await getHealthAggregate();
      setHealth(data.services);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : "Failed to load live health.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const packages = useMemo(
    () => (filter === "all" ? PLATFORM_PACKAGES : PLATFORM_PACKAGES.filter((pkg) => pkg.kind === filter)),
    [filter],
  );

  const coreHealthy = PLATFORM_PACKAGES.filter((p) => p.kind === "core" && p.healthService).filter((p) => {
    const h = statusFor(p, health);
    return h?.status.toLowerCase() === "healthy";
  }).length;
  const coreTotal = PLATFORM_PACKAGES.filter((p) => p.kind === "core" && p.healthService).length;
  const addonReachable = PLATFORM_PACKAGES.filter((p) => p.kind === "addon" && p.healthService).filter((p) => {
    const h = statusFor(p, health);
    return Boolean(h?.reachable && h.status.toLowerCase() !== "unreachable");
  }).length;
  const addonTotal = PLATFORM_PACKAGES.filter((p) => p.kind === "addon" && p.healthService).length;

  return (
    <>
      <PageHeader
        pretitle="Platform"
        title="Services & packages"
        actions={
          <button type="button" className="btn" onClick={() => void load()} disabled={loading}>
            Refresh health
          </button>
        }
      />
      <div className="page-body">
        <div className="container-xl">
          <ErrorAlert error={error} />
          <div className="row row-cards mb-3">
            <div className="col-sm-6 col-lg-3">
              <div className="card">
                <div className="card-body">
                  <div className="subheader">Core services</div>
                  <div className="h1 mb-0">
                    {coreHealthy}/{coreTotal}
                  </div>
                  <div className="text-secondary">healthy in lite stack</div>
                </div>
              </div>
            </div>
            <div className="col-sm-6 col-lg-3">
              <div className="card">
                <div className="card-body">
                  <div className="subheader">Add-on packages</div>
                  <div className="h1 mb-0">
                    {addonReachable}/{addonTotal}
                  </div>
                  <div className="text-secondary">reachable (need profile full)</div>
                </div>
              </div>
            </div>
            <div className="col-sm-12 col-lg-6">
              <div className="card">
                <div className="card-body">
                  <div className="subheader">How to enable add-ons</div>
                  <p className="mb-2">
                    Lite stack runs gateway, identity, user, coordinator, settings, admin. Add-ons
                    (audit, logging, location, file, notification + Mongo) and observability (Seq,
                    Jaeger, Prometheus, Grafana) start with Docker Compose profiles{" "}
                    <code>full</code> / <code>obs</code>.
                  </p>
                  <code className="d-block text-wrap">
                    docker compose ... --profile full --profile obs up -d
                  </code>
                </div>
              </div>
            </div>
          </div>

          <div className="btn-list mb-3">
            {(["all", "core", "addon", "observability"] as const).map((key) => (
              <button
                key={key}
                type="button"
                className={filter === key ? "btn btn-primary" : "btn"}
                onClick={() => setFilter(key)}
              >
                {key === "all" ? "All" : kindLabel(key)}
              </button>
            ))}
          </div>

          <div className="row row-cards">
            {packages.map((pkg) => {
              const live = statusFor(pkg, health);
              const badge =
                live == null
                  ? pkg.kind === "addon" || pkg.kind === "observability"
                    ? "Optional"
                    : "Infra"
                  : live.status;

              const badgeClass =
                live == null
                  ? "badge bg-secondary-lt"
                  : live.status.toLowerCase() === "healthy"
                    ? "badge bg-green-lt"
                    : live.reachable
                      ? "badge bg-yellow-lt"
                      : "badge bg-red-lt";

              return (
                <div className="col-md-6 col-xl-4" key={pkg.id}>
                  <div className="card">
                    <div className="card-body">
                      <div className="d-flex align-items-center mb-2">
                        <div className="subheader mb-0">{kindLabel(pkg.kind)}</div>
                        <span className={`${badgeClass} ms-auto`}>{badge}</span>
                      </div>
                      <h3 className="card-title mb-1">{pkg.name}</h3>
                      <p className="text-secondary">{pkg.summary}</p>
                      {pkg.gatewayPrefix ? (
                        <div className="mb-1">
                          <span className="text-secondary">API: </span>
                          <code>{pkg.gatewayPrefix}</code>
                        </div>
                      ) : null}
                      <div className="mb-2 small text-secondary">{pkg.composeNote}</div>
                      <div className="btn-list">
                        {pkg.adminPath ? (
                          <Link className="btn btn-sm" to={pkg.adminPath}>
                            Open in admin
                          </Link>
                        ) : null}
                        {pkg.id === "rabbitmq" ? (
                          <a className="btn btn-sm" href="http://localhost:15672" target="_blank" rel="noreferrer">
                            Management UI
                          </a>
                        ) : null}
                        {pkg.id === "seq" ? (
                          <a className="btn btn-sm" href="http://localhost:5341" target="_blank" rel="noreferrer">
                            Open Seq
                          </a>
                        ) : null}
                        {pkg.id === "jaeger" ? (
                          <a className="btn btn-sm" href="http://localhost:16686" target="_blank" rel="noreferrer">
                            Open Jaeger
                          </a>
                        ) : null}
                        {pkg.id === "grafana" ? (
                          <a className="btn btn-sm" href="http://localhost:3000" target="_blank" rel="noreferrer">
                            Open Grafana
                          </a>
                        ) : null}
                        {pkg.id === "prometheus" ? (
                          <a className="btn btn-sm" href="http://localhost:9090" target="_blank" rel="noreferrer">
                            Open Prometheus
                          </a>
                        ) : null}
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </>
  );
}
