import { useTranslation } from "react-i18next";
import { PageFrame } from "@/components/control";

/** @deprecated Prefer PageFrame — thin adapter for remaining callers */
export function PageHeader({
  pretitle,
  title,
  actions,
}: {
  pretitle: string;
  title: string;
  actions?: React.ReactNode;
}) {
  return <PageFrame pretitle={pretitle} title={title} actions={actions} />;
}

export function ErrorAlert({ error }: { error: string | null }) {
  if (!error) {
    return null;
  }

  return (
    <div className="alert alert-danger" role="alert">
      {error}
    </div>
  );
}

export function ServiceUnavailableAlert({ service }: { service: string }) {
  return (
    <div className="alert alert-warning" role="alert">
      <strong>{service}</strong> looks unavailable in this stack (lite profile may omit it, or the
      gateway could not reach it).
    </div>
  );
}

export function PaginationBar({
  page,
  totalPages,
  hasPrevious,
  hasNext,
  loading,
  onChange,
}: {
  page: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
  loading?: boolean;
  onChange: (page: number) => void;
}) {
  const { t } = useTranslation("common");

  if (totalPages <= 1) {
    return null;
  }

  return (
    <div className="card-footer d-flex justify-content-between align-items-center">
      <button
        type="button"
        className="btn"
        disabled={!hasPrevious || loading}
        onClick={() => onChange(Math.max(1, page - 1))}
      >
        {t("previous")}
      </button>
      <span className="text-secondary">
        {t("page")} {page}/{totalPages}
      </span>
      <button
        type="button"
        className="btn"
        disabled={!hasNext || loading}
        onClick={() => onChange(page + 1)}
      >
        {t("next")}
      </button>
    </div>
  );
}

export function FieldErrors({ failures }: { failures?: Record<string, string[]> }) {
  if (!failures || Object.keys(failures).length === 0) {
    return null;
  }

  return (
    <ul className="text-danger small mb-0">
      {Object.entries(failures).flatMap(([field, messages]) =>
        messages.map((message) => (
          <li key={`${field}-${message}`}>
            <strong>{field}:</strong> {message}
          </li>
        )),
      )}
    </ul>
  );
}
