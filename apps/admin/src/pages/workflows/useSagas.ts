import { useCallback, useEffect, useMemo, useState } from "react";
import { ApiClientError } from "@/api/client";
import { getSaga, listSagas } from "@/api/ops";
import type { SagaItem } from "@/api/types";
import type { SagaBoard } from "./catalog";

export function mapSagaToBoard(item: SagaItem, nowMs = Date.now()): SagaBoard {
  if (item.state === "Completed") return "Completed";
  if (item.state === "Failed") return "Failed";
  if (item.state === "Compensating") return "Compensated";
  if (item.lockedUntilUtc && new Date(item.lockedUntilUtc).getTime() > nowMs) return "Waiting";
  return "Running";
}

export function durationLabel(item: SagaItem): string {
  const start = new Date(item.createdAtUtc).getTime();
  const end = item.isTerminal && item.modifiedAtUtc ? new Date(item.modifiedAtUtc).getTime() : Date.now();
  const ms = Math.max(0, end - start);
  if (ms < 1000) return `${ms}ms`;
  return `${(ms / 1000).toFixed(1)}s`;
}

export function useSagaList(stateFilter?: string) {
  const [items, setItems] = useState<SagaItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await listSagas({ state: stateFilter, take: 100 });
      setItems(data.items ?? []);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.message : "Failed to load sagas.");
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, [stateFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  const byBoard = useMemo(() => {
    const map: Record<SagaBoard, SagaItem[]> = {
      Running: [],
      Completed: [],
      Failed: [],
      Compensated: [],
      Waiting: [],
      Retrying: [],
    };
    for (const item of items) {
      map[mapSagaToBoard(item)].push(item);
    }
    return map;
  }, [items]);

  return { items, byBoard, loading, error, load };
}

export function useSagaDetail(sagaId: string | undefined) {
  const [item, setItem] = useState<SagaItem | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!sagaId) {
      setItem(null);
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    void getSaga(sagaId)
      .then((data) => {
        if (!cancelled) setItem(data);
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof ApiClientError ? err.message : "Failed to load saga.");
          setItem(null);
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [sagaId]);

  return { item, loading, error };
}
