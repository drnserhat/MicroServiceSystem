import { useCallback, useEffect, useState } from "react";
import { ApiClientError } from "@/api/client";
import { getOutboxSnapshot, requeueDeadLetter, type OutboxService } from "@/api/ops";
import type { OutboxDeadLetter, OutboxPending, OutboxSummary } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { FrameworkPermissions } from "@/auth/permissionCodes";

export function useOutboxData(service: OutboxService = "identity") {
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
      try {
        await requeueDeadLetter(service, id);
        await load();
      } catch (err) {
        setError(err instanceof ApiClientError ? err.message : "Requeue failed.");
      }
    },
    [canWrite, load, service],
  );

  return { service, summary, deadLetters, pending, error, loading, canWrite, load, onRequeue, setError };
}
