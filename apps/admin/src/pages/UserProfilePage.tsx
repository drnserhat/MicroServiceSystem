import { useEffect, useState, type FormEvent } from "react";
import { Link, Navigate, useParams } from "react-router-dom";
import { ApiClientError } from "@/api/client";
import { getUserProfile, updateUserProfile } from "@/api/users";
import type { UserProfile } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { ErrorAlert, FieldErrors, PageHeader } from "@/components/ui";

export function UserProfilePage() {
  return (
    <RequirePermission permission={FrameworkPermissions.UsersProfilesRead}>
      <UserProfileInner />
    </RequirePermission>
  );
}

function UserProfileInner() {
  const { userId } = useParams<{ userId: string }>();
  const { can } = useAuth();
  const canWrite = can(FrameworkPermissions.UsersProfilesUpdate);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [failures, setFailures] = useState<Record<string, string[]> | undefined>();
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!userId) return;
    let cancelled = false;
    async function load() {
      setBusy(true);
      setError(null);
      try {
        const data = await getUserProfile(userId!);
        if (cancelled) return;
        setProfile(data);
        setFirstName(data.firstName);
        setLastName(data.lastName);
        setDisplayName(data.displayName);
      } catch (err) {
        if (cancelled) return;
        setProfile(null);
        setError(err instanceof ApiClientError ? err.message : "Load failed.");
      } finally {
        if (!cancelled) setBusy(false);
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, [userId]);

  if (!userId) {
    return <Navigate to="/users" replace />;
  }

  async function onSave(event: FormEvent) {
    event.preventDefault();
    if (!profile || !canWrite) return;
    setBusy(true);
    setError(null);
    setFailures(undefined);
    try {
      const data = await updateUserProfile(
        profile.id,
        { firstName, lastName, displayName: displayName || undefined },
        profile.version,
      );
      setProfile(data);
      setFirstName(data.firstName);
      setLastName(data.lastName);
      setDisplayName(data.displayName);
    } catch (err) {
      if (err instanceof ApiClientError) {
        setError(err.message);
        setFailures(err.failures);
      } else {
        setError("Update failed.");
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader
        pretitle="Identity"
        title="User profile"
        actions={
          <Link className="btn" to="/users">
            Back to users
          </Link>
        }
      />
      <div className="page-body">
        <div className="container-xl">
          <ErrorAlert error={error} />
          <FieldErrors failures={failures} />
          {busy && !profile ? <div className="text-secondary">Loading profile…</div> : null}
          {profile ? (
            <div className="card">
              <div className="card-header">
                <h3 className="card-title">
                  {profile.displayName}{" "}
                  <span className="badge bg-azure-lt ms-2">v{profile.version}</span>
                  {!profile.isActive ? <span className="badge bg-red-lt ms-2">Inactive</span> : null}
                </h3>
              </div>
              <form className="card-body row g-3" onSubmit={onSave}>
                <div className="col-md-4">
                  <label className="form-label">First name</label>
                  <input
                    className="form-control"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    disabled={!canWrite}
                    required
                  />
                </div>
                <div className="col-md-4">
                  <label className="form-label">Last name</label>
                  <input
                    className="form-control"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    disabled={!canWrite}
                    required
                  />
                </div>
                <div className="col-md-4">
                  <label className="form-label">Display name</label>
                  <input
                    className="form-control"
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    disabled={!canWrite}
                  />
                </div>
                {canWrite ? (
                  <div className="col-12">
                    <button type="submit" className="btn btn-primary" disabled={busy}>
                      Save
                    </button>
                  </div>
                ) : null}
              </form>
            </div>
          ) : null}
        </div>
      </div>
    </>
  );
}
