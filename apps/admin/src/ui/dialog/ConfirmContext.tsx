import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
} from "react";

export type ConfirmTone = "danger" | "primary" | "warning";

export type ConfirmOptions = {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  tone?: ConfirmTone;
};

export type PromptOptions = ConfirmOptions & {
  promptLabel: string;
  defaultValue?: string;
  placeholder?: string;
  required?: boolean;
};

type DialogState =
  | null
  | ({
      mode: "confirm";
      resolve: (value: boolean) => void;
    } & ConfirmOptions)
  | ({
      mode: "prompt";
      resolve: (value: string | null) => void;
    } & PromptOptions);

type ConfirmApi = {
  confirm: (options: ConfirmOptions) => Promise<boolean>;
  prompt: (options: PromptOptions) => Promise<string | null>;
};

const ConfirmContext = createContext<ConfirmApi | null>(null);

const toneBtn: Record<ConfirmTone, string> = {
  danger: "btn-danger",
  primary: "btn-primary",
  warning: "btn-warning",
};

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [dialog, setDialog] = useState<DialogState>(null);
  const [promptValue, setPromptValue] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const titleId = useId();

  const close = useCallback(() => setDialog(null), []);

  const confirm = useCallback((options: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => {
      setDialog({
        mode: "confirm",
        resolve,
        tone: "danger",
        confirmLabel: "Confirm",
        cancelLabel: "Cancel",
        ...options,
      });
    });
  }, []);

  const prompt = useCallback((options: PromptOptions) => {
    return new Promise<string | null>((resolve) => {
      setPromptValue(options.defaultValue ?? "");
      setDialog({
        mode: "prompt",
        resolve,
        tone: "danger",
        confirmLabel: "Confirm",
        cancelLabel: "Cancel",
        required: true,
        ...options,
      });
    });
  }, []);

  const api = useMemo<ConfirmApi>(() => ({ confirm, prompt }), [confirm, prompt]);

  useEffect(() => {
    if (!dialog) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        e.preventDefault();
        if (dialog.mode === "confirm") dialog.resolve(false);
        else dialog.resolve(null);
        close();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [dialog, close]);

  useEffect(() => {
    if (dialog?.mode === "prompt") {
      window.setTimeout(() => inputRef.current?.focus(), 0);
    }
  }, [dialog]);

  function cancel() {
    if (!dialog) return;
    if (dialog.mode === "confirm") dialog.resolve(false);
    else dialog.resolve(null);
    close();
  }

  function accept(event?: FormEvent) {
    event?.preventDefault();
    if (!dialog) return;
    if (dialog.mode === "confirm") {
      dialog.resolve(true);
      close();
      return;
    }
    const value = promptValue.trim();
    if (dialog.required !== false && !value) return;
    dialog.resolve(value || (dialog.defaultValue ?? ""));
    close();
  }

  return (
    <ConfirmContext.Provider value={api}>
      {children}
      {dialog ? (
        <div
          className="modal modal-blur fade show d-block msf-confirm-modal"
          style={{ background: "rgba(0,0,0,.55)" }}
          role="dialog"
          aria-modal="true"
          aria-labelledby={titleId}
          onClick={cancel}
        >
          <div
            className="modal-dialog modal-dialog-centered modal-sm"
            role="document"
            onClick={(e) => e.stopPropagation()}
          >
            <form className="modal-content" onSubmit={accept}>
              <div className="modal-header">
                <h3 className="modal-title" id={titleId}>
                  {dialog.title}
                </h3>
                <button type="button" className="btn-close" aria-label="Close" onClick={cancel} />
              </div>
              <div className="modal-body">
                <p className="mb-0 text-secondary">{dialog.message}</p>
                {dialog.mode === "prompt" ? (
                  <div className="mt-3">
                    <label className="form-label">{dialog.promptLabel}</label>
                    <input
                      ref={inputRef}
                      className="form-control"
                      value={promptValue}
                      placeholder={dialog.placeholder}
                      onChange={(e) => setPromptValue(e.target.value)}
                      required={dialog.required !== false}
                    />
                  </div>
                ) : null}
              </div>
              <div className="modal-footer">
                <button type="button" className="btn" onClick={cancel}>
                  {dialog.cancelLabel ?? "Cancel"}
                </button>
                <button type="submit" className={`btn ${toneBtn[dialog.tone ?? "danger"]}`}>
                  {dialog.confirmLabel ?? "Confirm"}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </ConfirmContext.Provider>
  );
}

export function useConfirm(): ConfirmApi {
  const ctx = useContext(ConfirmContext);
  if (!ctx) {
    throw new Error("useConfirm must be used within ConfirmProvider");
  }
  return ctx;
}
