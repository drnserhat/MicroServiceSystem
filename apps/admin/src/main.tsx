import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "@tabler/core/dist/css/tabler.min.css";
import "@tabler/core/dist/css/tabler-themes.min.css";
import "@tabler/core";
import "./styles/msf-tokens.css";
import "./i18n";
import App from "./App";
import { ThemeProvider } from "./theme/ThemeContext";
import { I18nextProvider } from "react-i18next";
import i18n from "./i18n";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <I18nextProvider i18n={i18n}>
      <ThemeProvider>
        <App />
      </ThemeProvider>
    </I18nextProvider>
  </StrictMode>,
);
