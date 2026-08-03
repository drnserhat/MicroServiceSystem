import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ApiClientError } from "@/api/client";
import { getHealthAggregate } from "@/api/ops";
import type { ServiceHealthItem } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import {
  FilterBar,
  MetricCard,
  PageFrame,
  ServiceCard,
  StatusBadge,
} from "@/components/control";
import { ErrorAlert } from "@/components/ui";
import { PLATFORM_PACKAGES, type PlatformPackage, type PlatformPackageKind } from "@/platform/catalog";
import { ExternalToolLink } from "@/platform/tools";

export function PlatformPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.OpsHealthRead}>
      <PlatformInner />
    </RequirePermission>
  );
}

function kindLabel(kind: PlatformPackageKind, t: (key: string) => string): string {
  switch (kind) {
    case "core":
      return t("kindCore");
    case "addon":
      return t("kindAddon");
    case "observability":
      return t("kindObservability");
  }
}

function statusFor(pkg: PlatformPackage, health: ServiceHealthItem[]): ServiceHealthItem | undefined {
  if (!pkg.healthService) return undefined;
  return health.find((item) => item.service === pkg.healthService);
}

const TOOL_BY_PACKAGE: Record<string, string> = {
  rabbitmq: "rabbitmq",
  seq: "seq",
  jaeger: "jaeger",
  grafana: "grafana",
  prometheus: "prometheus",
};

function PlatformInner() {
  const { t } = useTranslation(["platform", "common", "ops"]);
  const [health, setHealth] = useState<ServiceHealthItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<"all" | PlatformPackageKind>("all");
  const [query, setQuery] = useState("");

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await getHealthAggregate();
      setHealth(data.services);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : t("loadFailed"));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const packages = useMemo(() => {
    const byKind =
      filter === "all" ? PLATFORM_PACKAGES : PLATFORM_PACKAGES.filter((pkg) => pkg.kind === filter);
    const q = query.trim().toLowerCase();
    if (!q) return byKind;
    return byKind.filter(
      (pkg) =>
        pkg.name.toLowerCase().includes(q) ||
        pkg.id.toLowerCase().includes(q) ||
        pkg.summary.toLowerCase().includes(q),
    );
  }, [filter, query]);

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
    <PageFrame
      pretitle={t("pretitle")}
      title={t("titlePackages")}
      description={t("descriptionPackages")}
      actions={
        <div className="btn-list">
          <Link className="btn" to="/map">
            {t("platformMap")}
          </Link>
          <Link className="btn" to="/services">
            {t("serviceCenter")}
          </Link>
          <button type="button" className="btn" onClick={() => void load()} disabled={loading}>
            {t("refreshHealth")}
          </button>
        </div>
      }
    >
      <ErrorAlert error={error} />
      <div className="row row-cards mb-3">
        <div className="col-sm-6 col-lg-3">
          <MetricCard label={t("coreHealthy")} value={`${coreHealthy}/${coreTotal}`} tone="healthy" hint={t("liteStack")} />
        </div>
        <div className="col-sm-6 col-lg-3">
          <MetricCard
            label={t("addonsReachable")}
            value={`${addonReachable}/${addonTotal}`}
            tone={addonReachable > 0 ? "info" : "degraded"}
            hint={t("needProfileFull")}
          />
        </div>
        <div className="col-sm-12 col-lg-6">
          <div className="card h-100">
            <div className="card-body">
              <div className="subheader">Enable add-ons</div>
              <p className="mb-2 text-secondary">
                Lite: gateway, identity, user, coordinator, settings, admin. Profiles{" "}
                <code>full</code> / <code>obs</code> add packages and Seq/Jaeger/Prom/Grafana.
              </p>
              <code className="d-block text-wrap small">
                docker compose --profile full --profile obs up -d
              </code>
            </div>
          </div>
        </div>
      </div>

      <FilterBar
        search={query}
        onSearchChange={setQuery}
        searchPlaceholder={t("filterPackagesPlaceholder")}
        chips={[
          { id: "all", label: t("filterAll") },
          { id: "core", label: t("filterCore") },
          { id: "addon", label: t("filterAddon") },
          { id: "observability", label: t("filterObservability") },
        ]}
        activeChipId={filter}
        onChipChange={(id) => setFilter(id as "all" | PlatformPackageKind)}
      />

      <div className="row row-cards mt-3">
        {packages.map((pkg) => {
          const live = statusFor(pkg, health);
          const toolId = TOOL_BY_PACKAGE[pkg.id];
          return (
            <div className="col-md-6 col-xl-4" key={pkg.id}>
              <ServiceCard
                name={pkg.name}
                summary={pkg.summary}
                kind={kindLabel(pkg.kind, t)}
                status={live?.status}
                reachable={live?.reachable}
                actions={
                  <>
                    {pkg.gatewayPrefix ? (
                      <div className="mb-2 small">
                        <span className="text-secondary">API </span>
                        <code>{pkg.gatewayPrefix}</code>
                      </div>
                    ) : null}
                    <div className="small text-secondary mb-2">{pkg.composeNote}</div>
                    {!live && (pkg.kind === "addon" || pkg.kind === "observability") ? (
                      <div className="mb-2">
                        <StatusBadge tone="infra">{t("ops:optional")}</StatusBadge>
                      </div>
                    ) : null}
                    <div className="btn-list">
                      {pkg.adminPath ? (
                        <Link className="btn btn-sm" to={pkg.adminPath}>
                          {t("openInAdmin")}
                        </Link>
                      ) : null}
                      {pkg.healthService ? (
                        <Link className="btn btn-sm" to={`/services/${pkg.healthService}`}>
                          {t("service")}
                        </Link>
                      ) : null}
                      {toolId ? <ExternalToolLink id={toolId} className="btn btn-sm" /> : null}
                    </div>
                  </>
                }
              />
            </div>
          );
        })}
      </div>
    </PageFrame>
  );
}
