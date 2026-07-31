import { OUTBOX_SERVICES, type OutboxService } from "@/api/ops";

export function parseOutboxService(value: string | null): OutboxService {
  if (value && (OUTBOX_SERVICES as readonly string[]).includes(value)) {
    return value as OutboxService;
  }
  return "identity";
}

export function OutboxServiceSelect({
  value,
  onChange,
}: {
  value: OutboxService;
  onChange: (service: OutboxService) => void;
}) {
  return (
    <select
      className="form-select form-select-sm"
      style={{ maxWidth: 220 }}
      value={value}
      aria-label="Outbox service"
      onChange={(e) => onChange(parseOutboxService(e.target.value))}
    >
      {OUTBOX_SERVICES.map((service) => (
        <option key={service} value={service}>
          {service}
        </option>
      ))}
    </select>
  );
}
