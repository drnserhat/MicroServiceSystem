import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import {
  DEFAULT_LOCALE,
  detectInitialLocale,
  I18N_NAMESPACES,
  LOCALE_STORAGE_KEY,
  SUPPORTED_LOCALES,
  type AppLocale,
} from "./locales";

import enCommon from "./locales/en-US/common.json";
import enNav from "./locales/en-US/nav.json";
import enAuth from "./locales/en-US/auth.json";
import enUsers from "./locales/en-US/users.json";
import enRoles from "./locales/en-US/roles.json";
import enTenants from "./locales/en-US/tenants.json";
import enCountries from "./locales/en-US/countries.json";
import enSettings from "./locales/en-US/settings.json";
import enFiles from "./locales/en-US/files.json";
import enNotifications from "./locales/en-US/notifications.json";
import enAudit from "./locales/en-US/audit.json";
import enLogs from "./locales/en-US/logs.json";
import enPlatform from "./locales/en-US/platform.json";
import enOps from "./locales/en-US/ops.json";
import enMessaging from "./locales/en-US/messaging.json";
import enWorkflows from "./locales/en-US/workflows.json";
import enObservability from "./locales/en-US/observability.json";
import enArchitecture from "./locales/en-US/architecture.json";
import enDeveloper from "./locales/en-US/developer.json";
import enHome from "./locales/en-US/home.json";

import trCommon from "./locales/tr-TR/common.json";
import trNav from "./locales/tr-TR/nav.json";
import trAuth from "./locales/tr-TR/auth.json";
import trUsers from "./locales/tr-TR/users.json";
import trRoles from "./locales/tr-TR/roles.json";
import trTenants from "./locales/tr-TR/tenants.json";
import trCountries from "./locales/tr-TR/countries.json";
import trSettings from "./locales/tr-TR/settings.json";
import trFiles from "./locales/tr-TR/files.json";
import trNotifications from "./locales/tr-TR/notifications.json";
import trAudit from "./locales/tr-TR/audit.json";
import trLogs from "./locales/tr-TR/logs.json";
import trPlatform from "./locales/tr-TR/platform.json";
import trOps from "./locales/tr-TR/ops.json";
import trMessaging from "./locales/tr-TR/messaging.json";
import trWorkflows from "./locales/tr-TR/workflows.json";
import trObservability from "./locales/tr-TR/observability.json";
import trArchitecture from "./locales/tr-TR/architecture.json";
import trDeveloper from "./locales/tr-TR/developer.json";
import trHome from "./locales/tr-TR/home.json";

import zhCommon from "./locales/zh-CN/common.json";
import zhNav from "./locales/zh-CN/nav.json";
import zhAuth from "./locales/zh-CN/auth.json";
import zhUsers from "./locales/zh-CN/users.json";
import zhRoles from "./locales/zh-CN/roles.json";
import zhTenants from "./locales/zh-CN/tenants.json";
import zhCountries from "./locales/zh-CN/countries.json";
import zhSettings from "./locales/zh-CN/settings.json";
import zhFiles from "./locales/zh-CN/files.json";
import zhNotifications from "./locales/zh-CN/notifications.json";
import zhAudit from "./locales/zh-CN/audit.json";
import zhLogs from "./locales/zh-CN/logs.json";
import zhPlatform from "./locales/zh-CN/platform.json";
import zhOps from "./locales/zh-CN/ops.json";
import zhMessaging from "./locales/zh-CN/messaging.json";
import zhWorkflows from "./locales/zh-CN/workflows.json";
import zhObservability from "./locales/zh-CN/observability.json";
import zhArchitecture from "./locales/zh-CN/architecture.json";
import zhDeveloper from "./locales/zh-CN/developer.json";
import zhHome from "./locales/zh-CN/home.json";

import esCommon from "./locales/es-ES/common.json";
import esNav from "./locales/es-ES/nav.json";
import esAuth from "./locales/es-ES/auth.json";
import esUsers from "./locales/es-ES/users.json";
import esRoles from "./locales/es-ES/roles.json";
import esTenants from "./locales/es-ES/tenants.json";
import esCountries from "./locales/es-ES/countries.json";
import esSettings from "./locales/es-ES/settings.json";
import esFiles from "./locales/es-ES/files.json";
import esNotifications from "./locales/es-ES/notifications.json";
import esAudit from "./locales/es-ES/audit.json";
import esLogs from "./locales/es-ES/logs.json";
import esPlatform from "./locales/es-ES/platform.json";
import esOps from "./locales/es-ES/ops.json";
import esMessaging from "./locales/es-ES/messaging.json";
import esWorkflows from "./locales/es-ES/workflows.json";
import esObservability from "./locales/es-ES/observability.json";
import esArchitecture from "./locales/es-ES/architecture.json";
import esDeveloper from "./locales/es-ES/developer.json";
import esHome from "./locales/es-ES/home.json";

import hiCommon from "./locales/hi-IN/common.json";
import hiNav from "./locales/hi-IN/nav.json";
import hiAuth from "./locales/hi-IN/auth.json";
import hiUsers from "./locales/hi-IN/users.json";
import hiRoles from "./locales/hi-IN/roles.json";
import hiTenants from "./locales/hi-IN/tenants.json";
import hiCountries from "./locales/hi-IN/countries.json";
import hiSettings from "./locales/hi-IN/settings.json";
import hiFiles from "./locales/hi-IN/files.json";
import hiNotifications from "./locales/hi-IN/notifications.json";
import hiAudit from "./locales/hi-IN/audit.json";
import hiLogs from "./locales/hi-IN/logs.json";
import hiPlatform from "./locales/hi-IN/platform.json";
import hiOps from "./locales/hi-IN/ops.json";
import hiMessaging from "./locales/hi-IN/messaging.json";
import hiWorkflows from "./locales/hi-IN/workflows.json";
import hiObservability from "./locales/hi-IN/observability.json";
import hiArchitecture from "./locales/hi-IN/architecture.json";
import hiDeveloper from "./locales/hi-IN/developer.json";
import hiHome from "./locales/hi-IN/home.json";

const resources = {
  "en-US": {
    common: enCommon,
    nav: enNav,
    auth: enAuth,
    users: enUsers,
    roles: enRoles,
    tenants: enTenants,
    countries: enCountries,
    settings: enSettings,
    files: enFiles,
    notifications: enNotifications,
    audit: enAudit,
    logs: enLogs,
    platform: enPlatform,
    ops: enOps,
    messaging: enMessaging,
    workflows: enWorkflows,
    observability: enObservability,
    architecture: enArchitecture,
    developer: enDeveloper,
    home: enHome,
  },
  "tr-TR": {
    common: trCommon,
    nav: trNav,
    auth: trAuth,
    users: trUsers,
    roles: trRoles,
    tenants: trTenants,
    countries: trCountries,
    settings: trSettings,
    files: trFiles,
    notifications: trNotifications,
    audit: trAudit,
    logs: trLogs,
    platform: trPlatform,
    ops: trOps,
    messaging: trMessaging,
    workflows: trWorkflows,
    observability: trObservability,
    architecture: trArchitecture,
    developer: trDeveloper,
    home: trHome,
  },
  "zh-CN": {
    common: zhCommon,
    nav: zhNav,
    auth: zhAuth,
    users: zhUsers,
    roles: zhRoles,
    tenants: zhTenants,
    countries: zhCountries,
    settings: zhSettings,
    files: zhFiles,
    notifications: zhNotifications,
    audit: zhAudit,
    logs: zhLogs,
    platform: zhPlatform,
    ops: zhOps,
    messaging: zhMessaging,
    workflows: zhWorkflows,
    observability: zhObservability,
    architecture: zhArchitecture,
    developer: zhDeveloper,
    home: zhHome,
  },
  "es-ES": {
    common: esCommon,
    nav: esNav,
    auth: esAuth,
    users: esUsers,
    roles: esRoles,
    tenants: esTenants,
    countries: esCountries,
    settings: esSettings,
    files: esFiles,
    notifications: esNotifications,
    audit: esAudit,
    logs: esLogs,
    platform: esPlatform,
    ops: esOps,
    messaging: esMessaging,
    workflows: esWorkflows,
    observability: esObservability,
    architecture: esArchitecture,
    developer: esDeveloper,
    home: esHome,
  },
  "hi-IN": {
    common: hiCommon,
    nav: hiNav,
    auth: hiAuth,
    users: hiUsers,
    roles: hiRoles,
    tenants: hiTenants,
    countries: hiCountries,
    settings: hiSettings,
    files: hiFiles,
    notifications: hiNotifications,
    audit: hiAudit,
    logs: hiLogs,
    platform: hiPlatform,
    ops: hiOps,
    messaging: hiMessaging,
    workflows: hiWorkflows,
    observability: hiObservability,
    architecture: hiArchitecture,
    developer: hiDeveloper,
    home: hiHome,
  },
} as const;

const initialLocale = detectInitialLocale();

void i18n.use(initReactI18next).init({
  resources,
  lng: initialLocale,
  fallbackLng: DEFAULT_LOCALE,
  supportedLngs: [...SUPPORTED_LOCALES],
  ns: [...I18N_NAMESPACES],
  defaultNS: "common",
  interpolation: { escapeValue: false },
  returnNull: false,
});

applyDocumentLocale(initialLocale);

i18n.on("languageChanged", (lng) => {
  applyDocumentLocale(lng as AppLocale);
  try {
    localStorage.setItem(LOCALE_STORAGE_KEY, lng);
  } catch {
    /* ignore */
  }
});

export function applyDocumentLocale(locale: string): void {
  if (typeof document !== "undefined") {
    document.documentElement.lang = locale;
  }
}

export async function setAppLocale(locale: AppLocale): Promise<void> {
  await i18n.changeLanguage(locale);
}

export function getAppLocale(): AppLocale {
  const current = i18n.language;
  return (SUPPORTED_LOCALES as readonly string[]).includes(current)
    ? (current as AppLocale)
    : DEFAULT_LOCALE;
}

export default i18n;
