import { useState, type FormEvent } from "react";
import { ApiClientError } from "@/api/client";
import { registerUser } from "@/api/users";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { PageFrame } from "@/components/control";
import { ErrorAlert, FieldErrors } from "@/components/ui";

export function RegisterUserPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.RegistrationUsersCreate}>
      <RegisterUserInner />
    </RequirePermission>
  );
}

function RegisterUserInner() {
  const { session } = useAuth();
  const [email, setEmail] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [failures, setFailures] = useState<Record<string, string[]> | undefined>();
  const [success, setSuccess] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setFailures(undefined);
    setSuccess(null);
    try {
      const result = await registerUser({
        email: email.trim(),
        userName: userName.trim(),
        password,
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        displayName: displayName.trim() || undefined,
        tenantId: session!.tenantId,
      });
      setSuccess(`Registered ${result.email} (user ${result.userId}, saga ${result.sagaId})`);
      setEmail("");
      setUserName("");
      setPassword("");
      setFirstName("");
      setLastName("");
      setDisplayName("");
    } catch (err) {
      if (err instanceof ApiClientError) {
        setError(err.message);
        setFailures(err.failures);
      } else {
        setError("Registration failed.");
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <PageFrame pretitle="Identity" title="Register user"
    >
          <div className="card card-md">
            <div className="card-body">
              <ErrorAlert error={error} />
              <FieldErrors failures={failures} />
              {success ? <div className="alert alert-success">{success}</div> : null}
              <form onSubmit={onSubmit} className="row g-3">
                <div className="col-md-6">
                  <label className="form-label">Email</label>
                  <input className="form-control" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
                </div>
                <div className="col-md-6">
                  <label className="form-label">Username</label>
                  <input className="form-control" value={userName} onChange={(e) => setUserName(e.target.value)} required />
                </div>
                <div className="col-md-6">
                  <label className="form-label">Password</label>
                  <input className="form-control" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
                </div>
                <div className="col-md-6">
                  <label className="form-label">Display name</label>
                  <input className="form-control" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
                </div>
                <div className="col-md-6">
                  <label className="form-label">First name</label>
                  <input className="form-control" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
                </div>
                <div className="col-md-6">
                  <label className="form-label">Last name</label>
                  <input className="form-control" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
                </div>
                <div className="col-12">
                  <button type="submit" className="btn btn-primary" disabled={busy}>
                    {busy ? "Registering…" : "Start registration saga"}
                  </button>
                </div>
              </form>
            </div>
          </div>
    </PageFrame>

  );
}
