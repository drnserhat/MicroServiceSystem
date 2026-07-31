import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiClientError } from "@/api/client";
import { disableIdentityUser, listIdentityUsers } from "@/api/identityAdmin";
import type { IdentityUserItem } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { DataTableShell, FilterBar, PageFrame, TableSkeleton } from "@/components/control";
import { ErrorAlert, PaginationBar } from "@/components/ui";

export function UsersDirectoryPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.IdentityUsersRead}>
      <UsersInner />
    </RequirePermission>
  );
}

function UsersInner() {
  const { can } = useAuth();
  const canDisable = can(FrameworkPermissions.IdentityUsersDisable);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [items, setItems] = useState<IdentityUserItem[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [hasPrevious, setHasPrevious] = useState(false);
  const [hasNext, setHasNext] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [disableTarget, setDisableTarget] = useState<IdentityUserItem | null>(null);
  const [disableReason, setDisableReason] = useState("Disabled by admin");

  async function load(p = page) {
    setLoading(true);
    setError(null);
    try {
      const data = await listIdentityUsers(p, 20, search);
      setItems([...data.items]);
      setTotalPages(data.totalPages || 1);
      setHasPrevious(data.hasPreviousPage);
      setHasNext(data.hasNextPage);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : "Failed to load users.");
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  async function confirmDisable() {
    if (!disableTarget || !canDisable) return;
    try {
      await disableIdentityUser(disableTarget.id, disableReason.trim() || "Disabled by admin");
      setDisableTarget(null);
      await load(page);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : "Disable failed.");
    }
  }

  return (
    <PageFrame
        pretitle="Identity"
        title="Users"
        actions={
          can(FrameworkPermissions.RegistrationUsersCreate) ? (
            <Link className="btn btn-primary" to="/users/register">
              Register user
            </Link>
          ) : null
        }
    >
          <ErrorAlert error={error} />
          <FilterBar
            search={search}
            onSearchChange={setSearch}
            searchPlaceholder="Search email or username"
            trailing={
              <button
                type="button"
                className="btn btn-primary w-100"
                onClick={() => {
                  if (page === 1) void load(1);
                  else setPage(1);
                }}
              >
                Search
              </button>
            }
          />

          {disableTarget ? (
            <div className="card mb-3 border-danger">
              <div className="card-body">
                <h3 className="card-title">Disable {disableTarget.email}?</h3>
                <label className="form-label">Reason</label>
                <input
                  className="form-control mb-3"
                  value={disableReason}
                  onChange={(e) => setDisableReason(e.target.value)}
                />
                <button type="button" className="btn btn-danger me-2" onClick={() => void confirmDisable()}>
                  Confirm disable
                </button>
                <button type="button" className="btn" onClick={() => setDisableTarget(null)}>
                  Cancel
                </button>
              </div>
            </div>
          ) : null}

          <DataTableShell
            title="Directory"
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
                  <th>Email</th>
                  <th>Username</th>
                  <th>Status</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {loading ? <TableSkeleton rows={5} cols={4} /> : null}
                {!loading && items.length === 0 ? (
                  <tr>
                    <td colSpan={4} className="text-secondary">
                      No users found.
                    </td>
                  </tr>
                ) : null}
                {items.map((item) => (
                  <tr key={item.id}>
                    <td>{item.email}</td>
                    <td>{item.userName}</td>
                    <td>
                      <span className={item.isActive ? "badge bg-green-lt" : "badge bg-red-lt"}>
                        {item.isActive ? "Active" : "Disabled"}
                      </span>
                    </td>
                    <td className="text-nowrap">
                      <Link className="btn btn-sm" to={`/users/${item.id}`}>
                        Open profile
                      </Link>{" "}
                      {canDisable && item.isActive ? (
                        <button
                          type="button"
                          className="btn btn-sm btn-danger"
                          onClick={() => {
                            setDisableReason("Disabled by admin");
                            setDisableTarget(item);
                          }}
                        >
                          Disable
                        </button>
                      ) : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </DataTableShell>
    </PageFrame>

  );
}
