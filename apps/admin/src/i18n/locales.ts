export const SUPPORTED_LOCALES = ["en-US", "tr-TR", "zh-CN", "es-ES", "hi-IN"] as const;

export type AppLocale = (typeof SUPPORTED_LOCALES)[number];

export const DEFAULT_LOCALE: AppLocale = "en-US";

export const LOCALE_STORAGE_KEY = "msf.admin.locale";

export const LOCALE_LABELS: Record<AppLocale, string> = {
  "en-US": "English",
  "tr-TR": "Türkçe",
  "zh-CN": "中文",
  "es-ES": "Español",
  "hi-IN": "हिन्दी",
};

export const I18N_NAMESPACES = [
  "common",
  "nav",
  "auth",
  "users",
  "roles",
  "tenants",
  "countries",
  "settings",
  "files",
  "notifications",
  "audit",
  "logs",
  "platform",
  "ops",
  "messaging",
  "workflows",
  "observability",
  "architecture",
  "developer",
  "home",
] as const;

export function isAppLocale(value: string): value is AppLocale {
  return (SUPPORTED_LOCALES as readonly string[]).includes(value);
}

/** Map browser language tags (en, en-GB, zh, zh-TW, …) onto a supported app locale. */
export function resolveLocale(candidate: string | null | undefined): AppLocale {
  if (!candidate) return DEFAULT_LOCALE;
  const normalized = candidate.trim().replace(/_/g, "-");
  if (isAppLocale(normalized)) return normalized;

  const lower = normalized.toLowerCase();
  if (lower.startsWith("tr")) return "tr-TR";
  if (lower.startsWith("zh")) return "zh-CN";
  if (lower.startsWith("es")) return "es-ES";
  if (lower.startsWith("hi")) return "hi-IN";
  if (lower.startsWith("en")) return "en-US";
  return DEFAULT_LOCALE;
}

export function loadStoredLocale(): AppLocale {
  try {
    return resolveLocale(localStorage.getItem(LOCALE_STORAGE_KEY));
  } catch {
    return resolveLocale(typeof navigator !== "undefined" ? navigator.language : DEFAULT_LOCALE);
  }
}

export function detectInitialLocale(): AppLocale {
  try {
    const stored = localStorage.getItem(LOCALE_STORAGE_KEY);
    if (stored) return resolveLocale(stored);
  } catch {
    /* ignore */
  }
  return resolveLocale(typeof navigator !== "undefined" ? navigator.language : DEFAULT_LOCALE);
}
