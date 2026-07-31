import { useCallback, useEffect, useState } from "react";
import { ApiClientError } from "@/api/client";
import { getOutboxSnapshot, requeueDeadLetter, type OutboxService } from "@/api/ops";
import type { OutboxDeadLetter, OutboxPending, OutboxSummary } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";
import { useConfirm } from "@/ui/dialog/ConfirmContext";
import { useToast } from "@/ui/toast/ToastContext";

export function useOutboxData(service: OutboxService = "identity") {
  const toast = useToast();
  const { confirm } = useConfirm();
  const { can } = useAuth();
  const canWrite = can(FrameworkPermissions.OpsOutboxWrite);
  const [summary, setSummary] = useState<OutboxSummary | null>(null);
  const [deadLetters, setDeadLetters] = useState<OutboxDeadLetter[]>([]);
  const [pending, setPending] = useState<OutboxPending[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getOutboxSnapshot(service);
      setSummary(data.summary);
      setDeadLetters(data.deadLetters ?? []);
      setPending(data.pending ?? []);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : "Failed to load outbox.");
      setSummary(null);
      setDeadLetters([]);
      setPending([]);
    } finally {
      setLoading(false);
    }
  }, [service]);

  useEffect(() => {
    void load();
  }, [load]);

  const onRequeue = useCallback(
    async (id: string) => {
      if (!canWrite) return;
      const ok = await confirm({
        title: "Requeue dead letter",
        message: `Requeue message ${id} on ${service} outbox?`,
        confirmLabel: "Requeue",
        tone: "warning",
      });
      if (!ok) return;
      try {
        await requeueDeadLetter(service, id);
        toast.success(`Dead letter ${id.slice(0, 8)}… requeued.`);
        await load();
      } catch (err) {
        const msg = err instanceof ApiClientError ? err.message : "Requeue failed.";
        setError(msg);
        toast.error(msg);
      }
    },
    [canWrite, confirm, load, service, toast],
  );

  return { service, summary, deadLetters, pending, error, loading, canWrite, load, onRequeue, setError };
}
