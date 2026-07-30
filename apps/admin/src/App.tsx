import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "@/auth/AuthContext";
import { RequireAuth } from "@/auth/RequireAuth";
import { Skeleton } from "@/components/control";
import { AppLayout } from "@/layout/AppLayout";

const LoginPage = lazy(() => import("@/pages/LoginPage").then((m) => ({ default: m.LoginPage })));
const HomePage = lazy(() => import("@/pages/HomePage").then((m) => ({ default: m.HomePage })));
const PlatformPage = lazy(() => import("@/pages/PlatformPage").then((m) => ({ default: m.PlatformPage })));
const SettingsPage = lazy(() => import("@/pages/SettingsPage").then((m) => ({ default: m.SettingsPage })));
const RegisterUserPage = lazy(() =>
  import("@/pages/RegisterUserPage").then((m) => ({ default: m.RegisterUserPage })),
);
const UserProfilePage = lazy(() =>
  import("@/pages/UserProfilePage").then((m) => ({ default: m.UserProfilePage })),
);
const UsersDirectoryPage = lazy(() =>
  import("@/pages/UsersDirectoryPage").then((m) => ({ default: m.UsersDirectoryPage })),
);
const TenantsPage = lazy(() => import("@/pages/TenantsPage").then((m) => ({ default: m.TenantsPage })));
const RolesPage = lazy(() => import("@/pages/RolesPage").then((m) => ({ default: m.RolesPage })));
const AuditPage = lazy(() => import("@/pages/AuditPage").then((m) => ({ default: m.AuditPage })));
const LogsPage = lazy(() => import("@/pages/LogsPage").then((m) => ({ default: m.LogsPage })));
const CountriesPage = lazy(() => import("@/pages/CountriesPage").then((m) => ({ default: m.CountriesPage })));
const FilesPage = lazy(() => import("@/pages/FilesPage").then((m) => ({ default: m.FilesPage })));
const NotificationsPage = lazy(() =>
  import("@/pages/NotificationsPage").then((m) => ({ default: m.NotificationsPage })),
);
const HealthPage = lazy(() => import("@/pages/HealthPage").then((m) => ({ default: m.HealthPage })));
const MessagingPage = lazy(() => import("@/pages/MessagingPage").then((m) => ({ default: m.MessagingPage })));
const ServicesPage = lazy(() => import("@/pages/ServicesPage").then((m) => ({ default: m.ServicesPage })));
const WorkflowsPage = lazy(() => import("@/pages/WorkflowsPage").then((m) => ({ default: m.WorkflowsPage })));
const ObservabilityPage = lazy(() =>
  import("@/pages/ObservabilityPage").then((m) => ({ default: m.ObservabilityPage })),
);
const ArchitecturePage = lazy(() =>
  import("@/pages/ArchitecturePage").then((m) => ({ default: m.ArchitecturePage })),
);
const BuildingBlocksPage = lazy(() =>
  import("@/pages/BuildingBlocksPage").then((m) => ({ default: m.BuildingBlocksPage })),
);
const DeveloperPage = lazy(() => import("@/pages/DeveloperPage").then((m) => ({ default: m.DeveloperPage })));

function RouteFallback() {
  return (
    <div className="container-xl py-4">
      <Skeleton height={48} className="mb-3" />
      <div className="row row-cards">
        <div className="col-md-4">
          <Skeleton height={120} />
        </div>
        <div className="col-md-4">
          <Skeleton height={120} />
        </div>
        <div className="col-md-4">
          <Skeleton height={120} />
        </div>
      </div>
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Suspense fallback={<RouteFallback />}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<RequireAuth />}>
              <Route element={<AppLayout />}>
                <Route index element={<HomePage />} />
                <Route path="platform" element={<PlatformPage />} />
                <Route path="services" element={<ServicesPage />} />
                <Route path="services/:serviceId" element={<ServicesPage />} />
                <Route path="workflows" element={<WorkflowsPage />} />
                <Route path="observability" element={<ObservabilityPage />} />
                <Route path="architecture" element={<ArchitecturePage />} />
                <Route path="building-blocks" element={<BuildingBlocksPage />} />
                <Route path="developer" element={<DeveloperPage />} />
                <Route path="settings" element={<SettingsPage />} />
                <Route path="users/register" element={<RegisterUserPage />} />
                <Route path="users/:userId" element={<UserProfilePage />} />
                <Route path="users" element={<UsersDirectoryPage />} />
                <Route path="tenants" element={<TenantsPage />} />
                <Route path="roles" element={<RolesPage />} />
                <Route path="audit" element={<AuditPage />} />
                <Route path="logs" element={<LogsPage />} />
                <Route path="countries" element={<CountriesPage />} />
                <Route path="files" element={<FilesPage />} />
                <Route path="notifications" element={<NotificationsPage />} />
                <Route path="health" element={<HealthPage />} />
                <Route path="messaging" element={<MessagingPage />} />
              </Route>
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Suspense>
      </BrowserRouter>
    </AuthProvider>
  );
}
