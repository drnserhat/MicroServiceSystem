import { useEffect, useState } from "react";
import { ApiClientError } from "@/api/client";
import { listRoles } from "@/api/identityAdmin";
import type { RoleItem } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { PageFrame, Skeleton } from "@/components/control";
import { ErrorAlert } from "@/components/ui";

export function RolesPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.IdentityRolesRead}>
      <RolesInner />
    </RequirePermission>
  );
}

function RolesInner() {
  const [items, setItems] = useState<RoleItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setLoading(true);
      try {
        const data = await listRoles();
        if (!cancelled) setItems(data);
      } catch (err) {
        if (!cancelled) setError(err instanceof ApiClientError ? err.message : "Failed to load roles.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <PageFrame
      pretitle="Identity"
      title="Roles & permissions"
    >
          <ErrorAlert error={error} />
          {loading ? <Skeleton height={120} className="mb-3" /> : null}
          <div className="row row-cards">
            {items.map((role) => (
              <div className="col-md-6" key={role.id}>
                <div className="card">
                  <div className="card-header">
                    <h3 className="card-title">{role.name}</h3>
                  </div>
                  <div className="card-body">
                    <div className="d-flex flex-wrap gap-1">
                      {role.permissions.map((permission) => (
                        <span key={permission} className="badge bg-azure-lt">
                          {permission}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
    </PageFrame>
  );
}
