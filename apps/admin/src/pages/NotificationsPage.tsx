import { useState, type FormEvent } from "react";
import { ApiClientError, isServiceUnavailable } from "@/api/client";
import { createNotification } from "@/api/notifications";
import type { IdentityUserItem } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { PageFrame } from "@/components/control";
import { UserPicker } from "@/components/UserPicker";
import { ErrorAlert, ServiceUnavailableAlert } from "@/components/ui";
import { useToast } from "@/ui/toast/ToastContext";

export function NotificationsPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.NotificationMessagesCreate}>
      <NotificationsInner />
    </RequirePermission>
  );
}

function NotificationsInner() {
  const toast = useToast();
  const [selected, setSelected] = useState<IdentityUserItem | null>(null);
  const [channel, setChannel] = useState("email");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [unavailable, setUnavailable] = useState(false);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!selected) return;
    setBusy(true);
    setError(null);
    setSuccess(false);
    setUnavailable(false);
    try {
      await createNotification({
        userId: selected.id,
        email: selected.email,
        displayName: selected.userName,
        channel: channel.trim(),
      });
      setSuccess(true);
      toast.success(`Notification queued for ${selected.email}.`);
    } catch (err) {
      if (isServiceUnavailable(err)) setUnavailable(true);
      else {
        const msg = err instanceof ApiClientError ? err.message : "Failed to send.";
        setError(msg);
        toast.error(msg);
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <PageFrame pretitle="Tools" title="Send notification"
    >
          {unavailable ? <ServiceUnavailableAlert service="Notification" /> : null}
          <ErrorAlert error={error} />
          {success ? <div className="alert alert-success">Notification accepted.</div> : null}
          <form className="card card-md" onSubmit={onSubmit}>
            <div className="card-body row g-3">
              <div className="col-12">
                <UserPicker
                  value={selected?.id ?? ""}
                  onChange={setSelected}
                  label="Recipient"
                />
              </div>
              {selected ? (
                <div className="col-12">
                  <div className="alert alert-info mb-0">
                    Will notify <strong>{selected.email}</strong> ({selected.userName})
                  </div>
                </div>
              ) : null}
              <div className="col-md-6">
                <label className="form-label">Channel</label>
                <select className="form-select" value={channel} onChange={(e) => setChannel(e.target.value)}>
                  <option value="email">email</option>
                  <option value="sms">sms</option>
                  <option value="push">push</option>
                </select>
              </div>
              <div className="col-12">
                <button type="submit" className="btn btn-primary" disabled={busy || !selected}>
                  {busy ? "Sending…" : "Send"}
                </button>
              </div>
            </div>
          </form>
    </PageFrame>

  );
}
