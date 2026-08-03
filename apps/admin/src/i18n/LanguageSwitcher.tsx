import { useTranslation } from "react-i18next";
import { LOCALE_LABELS, SUPPORTED_LOCALES, type AppLocale } from "./locales";
import { getAppLocale, setAppLocale } from "./index";

export function LanguageSwitcher() {
  const { t, i18n } = useTranslation("common");
  const current = getAppLocale();

  return (
    <label className="d-flex align-items-center gap-1 mb-0" title={t("language")}>
      <span className="visually-hidden">{t("language")}</span>
      <select
        className="form-select form-select-sm"
        style={{ width: "auto", minWidth: 108 }}
        value={current}
        aria-label={t("language")}
        onChange={(event) => {
          const next = event.target.value as AppLocale;
          void setAppLocale(next);
        }}
      >
        {SUPPORTED_LOCALES.map((locale) => (
          <option key={locale} value={locale}>
            {LOCALE_LABELS[locale]}
          </option>
        ))}
      </select>
      <span className="d-none">{i18n.language}</span>
    </label>
  );
}
