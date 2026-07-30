export type HealthTone = "healthy" | "degraded" | "critical" | "unknown" | "info" | "messaging" | "infra";

export function toneFromStatus(status?: string | null, reachable?: boolean): HealthTone {
  if (!status) return "unknown";
  const s = status.toLowerCase();
  if (s === "healthy" || s === "ok") return "healthy";
  if (s === "degraded" || s === "warning") return "degraded";
  if (s === "unhealthy" || s === "critical" || s === "unreachable") return "critical";
  if (reachable === false) return "critical";
  return "unknown";
}

export function badgeClass(tone: HealthTone): string {
  switch (tone) {
    case "healthy":
      return "badge bg-green-lt";
    case "degraded":
      return "badge bg-orange-lt";
    case "critical":
      return "badge bg-red-lt";
    case "info":
      return "badge bg-blue-lt";
    case "messaging":
      return "badge bg-purple-lt";
    case "infra":
      return "badge bg-secondary-lt";
    default:
      return "badge bg-secondary-lt";
  }
}
