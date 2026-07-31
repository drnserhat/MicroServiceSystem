import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "@tabler/core/dist/css/tabler.min.css";
import "@tabler/core/dist/css/tabler-themes.min.css";
import "@tabler/core";
import "./styles/msf-tokens.css";
import App from "./App";
import { ThemeProvider } from "./theme/ThemeContext";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <ThemeProvider>
      <App />
    </ThemeProvider>
  </StrictMode>,
);
