import { useState, type FormEvent } from "react";
import { ApiClientError, isServiceUnavailable } from "@/api/client";
import { uploadFile } from "@/api/files";
import type { FileAsset } from "@/api/types";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { RequirePermission } from "@/auth/RequirePermission";
import { PageFrame } from "@/components/control";
import { ErrorAlert, ServiceUnavailableAlert } from "@/components/ui";

export function FilesPage() {
  return (
    <RequirePermission permission={FrameworkPermissions.FileAssetsUpload}>
      <FilesInner />
    </RequirePermission>
  );
}

function FilesInner() {
  const [container, setContainer] = useState("admin");
  const [file, setFile] = useState<File | null>(null);
  const [result, setResult] = useState<FileAsset | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [unavailable, setUnavailable] = useState(false);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!file) return;
    setBusy(true);
    setError(null);
    setUnavailable(false);
    setResult(null);
    try {
      setResult(await uploadFile(file, container.trim() || "admin"));
    } catch (err) {
      if (isServiceUnavailable(err)) setUnavailable(true);
      else setError(err instanceof ApiClientError ? err.message : "Upload failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <PageFrame pretitle="Tools" title="File upload"
    >
          {unavailable ? <ServiceUnavailableAlert service="File" /> : null}
          <ErrorAlert error={error} />
          <form className="card card-md" onSubmit={onSubmit}>
            <div className="card-body">
              <div className="mb-3">
                <label className="form-label">Container</label>
                <input className="form-control" value={container} onChange={(e) => setContainer(e.target.value)} required />
              </div>
              <div className="mb-3">
                <label className="form-label">File</label>
                <input
                  className="form-control"
                  type="file"
                  onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                  required
                />
              </div>
              <button type="submit" className="btn btn-primary" disabled={busy || !file}>
                {busy ? "Uploading…" : "Upload"}
              </button>
            </div>
          </form>
          {result ? (
            <div className="card mt-3">
              <div className="card-body">
                <div>
                  <strong>{result.fileName}</strong> → <code>{result.path}</code>
                </div>
                <div className="text-secondary">
                  {result.sizeInBytes} bytes · {result.storageProvider} · {result.container}
                </div>
              </div>
            </div>
          ) : null}
    </PageFrame>

  );
}
