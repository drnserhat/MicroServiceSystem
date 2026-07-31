import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { ApiClientError } from "@/api/client";
import { useAuth } from "@/auth/AuthContext";
import { useToast } from "@/ui/toast/ToastContext";

export function LoginPage() {
  const toast = useToast();
  const { isAuthenticated, login, defaultTenantId } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/";

  const [email, setEmail] = useState("admin@dev.local");
  const [password, setPassword] = useState("DevAdmin!Pass1");
  const [tenantId, setTenantId] = useState(defaultTenantId);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      await login(email.trim(), password, tenantId.trim());
      toast.success("Signed in.");
      navigate(from, { replace: true });
    } catch (err) {
      const msg = err instanceof ApiClientError ? err.message : "Login failed.";
      setError(msg);
      toast.error(msg);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="page page-center">
      <div className="container container-tight py-4">
        <div className="text-center mb-4">
          <a href="." className="navbar-brand navbar-brand-autodark fw-bold fs-2">
            MSF Admin
          </a>
          <div className="text-secondary">Sign in to MicroServiceSystem Admin</div>
        </div>
        <div className="card card-md">
          <div className="card-body">
            <h2 className="h2 text-center mb-4">Login to your account</h2>
            {error ? (
              <div className="alert alert-danger" role="alert">
                {error}
              </div>
            ) : null}
            <form onSubmit={onSubmit} autoComplete="on">
              <div className="mb-3">
                <label className="form-label" htmlFor="email">
                  Email address
                </label>
                <input
                  id="email"
                  type="email"
                  className="form-control"
                  placeholder="you@company.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </div>
              <div className="mb-3">
                <label className="form-label" htmlFor="password">
                  Password
                </label>
                <input
                  id="password"
                  type="password"
                  className="form-control"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
              </div>
              <div className="mb-3">
                <label className="form-label" htmlFor="tenantId">
                  Tenant ID
                </label>
                <input
                  id="tenantId"
                  type="text"
                  className="form-control"
                  value={tenantId}
                  onChange={(e) => setTenantId(e.target.value)}
                  required
                />
              </div>
              <div className="form-footer">
                <button type="submit" className="btn btn-primary w-100" disabled={busy}>
                  {busy ? "Signing in…" : "Sign in"}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}
