import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { fileURLToPath, URL } from "node:url";

const gateway = "http://localhost:8080";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      "/identity": { target: gateway, changeOrigin: true },
      "/settings": { target: gateway, changeOrigin: true },
      "/user": { target: gateway, changeOrigin: true },
      "/location": { target: gateway, changeOrigin: true },
      "/audit": { target: gateway, changeOrigin: true },
      "/logging": { target: gateway, changeOrigin: true },
      "/coordinator": { target: gateway, changeOrigin: true },
      "/registration": { target: gateway, changeOrigin: true },
      "/notification": { target: gateway, changeOrigin: true },
      "/file": { target: gateway, changeOrigin: true },
      "/ops": { target: gateway, changeOrigin: true },
    },
  },
});
