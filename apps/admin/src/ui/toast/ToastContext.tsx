import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useTranslation } from "react-i18next";

export type ToastTone = "success" | "danger" | "warning" | "info";

export type ToastInput = {
  message: string;
  title?: string;
  tone?: ToastTone;
  durationMs?: number;
};

type ToastItem = Required<Pick<ToastInput, "message">> & {
  id: string;
  title?: string;
  tone: ToastTone;
  durationMs: number;
};

type ToastApi = {
  push: (input: ToastInput) => void;
  success: (message: string, title?: string) => void;
  error: (message: string, title?: string) => void;
  info: (message: string, title?: string) => void;
  warning: (message: string, title?: string) => void;
  dismiss: (id: string) => void;
};

const ToastContext = createContext<ToastApi | null>(null);

const DEFAULT_MS = 4200;

export function ToastProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation("common");
  const [items, setItems] = useState<ToastItem[]>([]);
  const timers = useRef<Map<string, number>>(new Map());

  const dismiss = useCallback((id: string) => {
    const timer = timers.current.get(id);
    if (timer) {
      window.clearTimeout(timer);
      timers.current.delete(id);
    }
    setItems((prev) => prev.filter((toast) => toast.id !== id));
  }, []);

  const push = useCallback(
    (input: ToastInput) => {
      const id = crypto.randomUUID();
      const durationMs = input.durationMs ?? DEFAULT_MS;
      const item: ToastItem = {
        id,
        message: input.message,
        title: input.title,
        tone: input.tone ?? "info",
        durationMs,
      };
      setItems((prev) => [...prev.slice(-4), item]);
      const timer = window.setTimeout(() => dismiss(id), durationMs);
      timers.current.set(id, timer);
    },
    [dismiss],
  );

  const api = useMemo<ToastApi>(
    () => ({
      push,
      dismiss,
      success: (message, title) => push({ message, title: title ?? t("success"), tone: "success" }),
      error: (message, title) => push({ message, title: title ?? t("error"), tone: "danger" }),
      info: (message, title) => push({ message, title, tone: "info" }),
      warning: (message, title) => push({ message, title: title ?? t("warning"), tone: "warning" }),
    }),
    [push, dismiss, t],
  );

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="msf-toast-stack" aria-live="polite" aria-relevant="additions">
        {items.map((item) => (
          <div
            key={item.id}
            className={`msf-toast alert alert-${item.tone} alert-dismissible mb-0`}
            role="status"
          >
            {item.title ? <h4 className="alert-title">{item.title}</h4> : null}
            <div className="text-secondary">{item.message}</div>
            <button
              type="button"
              className="btn-close"
              aria-label={t("dismiss")}
              onClick={() => dismiss(item.id)}
            />
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error("useToast must be used within ToastProvider");
  }
  return ctx;
}
