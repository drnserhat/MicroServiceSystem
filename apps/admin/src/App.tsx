import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "@/auth/AuthContext";
import { RequireAuth } from "@/auth/RequireAuth";
import { Skeleton } from "@/components/control";
import { AppLayout } from "@/layout/AppLayout";
import { ToastProvider } from "@/ui/toast/ToastContext";
import { ConfirmProvider } from "@/ui/dialog/ConfirmContext";

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
const MessagingPage = lazy(() => import("@/pages/MessagingPage").then((m) => ({ default: m.MessagingPage })));
const MessagingOverviewPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingOverviewPage })),
);
const MessagingQueuesPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingQueuesPage })),
);
const MessagingExchangesPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingExchangesPage })),
);
const MessagingBindingsPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingBindingsPage })),
);
const MessagingPublishersPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingPublishersPage })),
);
const MessagingConsumersPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingConsumersPage })),
);
const MessagingDeadLettersPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingDeadLettersPage })),
);
const MessagingOutboxPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingOutboxPage })),
);
const MessagingInboxPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingInboxPage })),
);
const MessagingEventFlowPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingEventFlowPage })),
);
const MessagingRetriesPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingRetriesPage })),
);
const MessagingReplayPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingReplayPage })),
);
const MessagingInspectPage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingInspectPage })),
);
const MessagingTimelinePage = lazy(() =>
  import("@/pages/messaging/pages").then((m) => ({ default: m.MessagingTimelinePage })),
);
const ServicesPage = lazy(() => import("@/pages/ServicesPage").then((m) => ({ default: m.ServicesPage })));
const WorkflowsPage = lazy(() => import("@/pages/WorkflowsPage").then((m) => ({ default: m.WorkflowsPage })));
const WorkflowsOverviewPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsOverviewPage })),
);
const WorkflowsBoardsPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsBoardsPage })),
);
const WorkflowsDefinitionsPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsDefinitionsPage })),
);
const WorkflowsRunningPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsRunningPage })),
);
const WorkflowsCompletedPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsCompletedPage })),
);
const WorkflowsFailedPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsFailedPage })),
);
const WorkflowsCompensatedPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsCompensatedPage })),
);
const WorkflowsWaitingPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsWaitingPage })),
);
const WorkflowsRetryingPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsRetryingPage })),
);
const WorkflowsDetailPage = lazy(() =>
  import("@/pages/workflows/pages").then((m) => ({ default: m.WorkflowsDetailPage })),
);
const ObservabilityPage = lazy(() =>
  import("@/pages/ObservabilityPage").then((m) => ({ default: m.ObservabilityPage })),
);
const ObservabilityOverviewPage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityOverviewPage })),
);
const ObservabilityMetricsPage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityMetricsPage })),
);
const ObservabilityTracingPage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityTracingPage })),
);
const ObservabilityLogsHubPage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityLogsPage })),
);
const ObservabilityAuditHubPage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityAuditPage })),
);
const ObservabilityErrorsPage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityErrorsPage })),
);
const ObservabilityPerformancePage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityPerformancePage })),
);
const ObservabilityOtelPage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityOtelPage })),
);
const ObservabilityPrometheusPage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityPrometheusPage })),
);
const ObservabilityCorrelationPage = lazy(() =>
  import("@/pages/observability/pages").then((m) => ({ default: m.ObservabilityCorrelationPage })),
);
const ArchitecturePage = lazy(() =>
  import("@/pages/ArchitecturePage").then((m) => ({ default: m.ArchitecturePage })),
);
const ArchitectureOverviewPage = lazy(() =>
  import("@/pages/architecture/pages").then((m) => ({ default: m.ArchitectureOverviewPage })),
);
const ArchitectureContextsPage = lazy(() =>
  import("@/pages/architecture/pages").then((m) => ({ default: m.ArchitectureContextsPage })),
);
const ArchitectureDependenciesPage = lazy(() =>
  import("@/pages/architecture/pages").then((m) => ({ default: m.ArchitectureDependenciesPage })),
);
const ArchitectureEventsPage = lazy(() =>
  import("@/pages/architecture/pages").then((m) => ({ default: m.ArchitectureEventsPage })),
);
const ArchitectureEventFlowPage = lazy(() =>
  import("@/pages/architecture/pages").then((m) => ({ default: m.ArchitectureEventFlowPage })),
);
const ArchitectureDatabasesPage = lazy(() =>
  import("@/pages/architecture/pages").then((m) => ({ default: m.ArchitectureDatabasesPage })),
);
const ArchitectureContractsPage = lazy(() =>
  import("@/pages/architecture/pages").then((m) => ({ default: m.ArchitectureContractsPage })),
);
const BuildingBlocksPage = lazy(() =>
  import("@/pages/BuildingBlocksPage").then((m) => ({ default: m.BuildingBlocksPage })),
);
const DeveloperPage = lazy(() => import("@/pages/DeveloperPage").then((m) => ({ default: m.DeveloperPage })));
const DeveloperOverviewPage = lazy(() =>
  import("@/pages/developer/pages").then((m) => ({ default: m.DeveloperOverviewPage })),
);
const DeveloperWizardPage = lazy(() =>
  import("@/pages/developer/pages").then((m) => ({ default: m.DeveloperWizardPage })),
);
const PlatformMapPage = lazy(() =>
  import("@/pages/PlatformMapPage").then((m) => ({ default: m.PlatformMapPage })),
);

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
    <ToastProvider>
    <ConfirmProvider>
    <AuthProvider>
      <BrowserRouter>
        <Suspense fallback={<RouteFallback />}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<RequireAuth />}>
              <Route element={<AppLayout />}>
                <Route index element={<HomePage />} />
                <Route path="map" element={<PlatformMapPage />} />
                <Route path="platform" element={<PlatformPage />} />
                <Route path="services" element={<ServicesPage />} />
                <Route path="services/:serviceId" element={<ServicesPage />} />
                <Route path="workflows" element={<WorkflowsPage />}>
                  <Route index element={<WorkflowsOverviewPage />} />
                  <Route path="boards" element={<WorkflowsBoardsPage />} />
                  <Route path="definitions" element={<WorkflowsDefinitionsPage />} />
                  <Route path="running" element={<WorkflowsRunningPage />} />
                  <Route path="completed" element={<WorkflowsCompletedPage />} />
                  <Route path="failed" element={<WorkflowsFailedPage />} />
                  <Route path="compensated" element={<WorkflowsCompensatedPage />} />
                  <Route path="waiting" element={<WorkflowsWaitingPage />} />
                  <Route path="retrying" element={<WorkflowsRetryingPage />} />
                  <Route path=":sagaId" element={<WorkflowsDetailPage />} />
                </Route>
                <Route path="observability" element={<ObservabilityPage />}>
                  <Route index element={<ObservabilityOverviewPage />} />
                  <Route path="metrics" element={<ObservabilityMetricsPage />} />
                  <Route path="tracing" element={<ObservabilityTracingPage />} />
                  <Route path="logs" element={<ObservabilityLogsHubPage />} />
                  <Route path="audit" element={<ObservabilityAuditHubPage />} />
                  <Route path="errors" element={<ObservabilityErrorsPage />} />
                  <Route path="performance" element={<ObservabilityPerformancePage />} />
                  <Route path="otel" element={<ObservabilityOtelPage />} />
                  <Route path="prometheus" element={<ObservabilityPrometheusPage />} />
                  <Route path="correlation" element={<ObservabilityCorrelationPage />} />
                </Route>
                <Route path="architecture" element={<ArchitecturePage />}>
                  <Route index element={<ArchitectureOverviewPage />} />
                  <Route path="contexts" element={<ArchitectureContextsPage />} />
                  <Route path="dependencies" element={<ArchitectureDependenciesPage />} />
                  <Route path="events" element={<ArchitectureEventsPage />} />
                  <Route path="event-flow" element={<ArchitectureEventFlowPage />} />
                  <Route path="databases" element={<ArchitectureDatabasesPage />} />
                  <Route path="contracts" element={<ArchitectureContractsPage />} />
                </Route>
                <Route path="building-blocks" element={<BuildingBlocksPage />} />
                <Route path="building-blocks/:blockId" element={<BuildingBlocksPage />} />
                <Route path="developer" element={<DeveloperPage />}>
                  <Route index element={<DeveloperOverviewPage />} />
                  <Route path=":wizardId" element={<DeveloperWizardPage />} />
                </Route>
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
                <Route path="health" element={<Navigate to="/map" replace />} />
                <Route path="messaging" element={<MessagingPage />}>
                  <Route index element={<MessagingOverviewPage />} />
                  <Route path="queues" element={<MessagingQueuesPage />} />
                  <Route path="exchanges" element={<MessagingExchangesPage />} />
                  <Route path="bindings" element={<MessagingBindingsPage />} />
                  <Route path="publishers" element={<MessagingPublishersPage />} />
                  <Route path="consumers" element={<MessagingConsumersPage />} />
                  <Route path="dead-letters" element={<MessagingDeadLettersPage />} />
                  <Route path="outbox" element={<MessagingOutboxPage />} />
                  <Route path="inbox" element={<MessagingInboxPage />} />
                  <Route path="event-flow" element={<MessagingEventFlowPage />} />
                  <Route path="retries" element={<MessagingRetriesPage />} />
                  <Route path="replay" element={<MessagingReplayPage />} />
                  <Route path="inspect" element={<MessagingInspectPage />} />
                  <Route path="timeline" element={<MessagingTimelinePage />} />
                </Route>
              </Route>
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Suspense>
      </BrowserRouter>
    </AuthProvider>
    </ConfirmProvider>
    </ToastProvider>
  );
}
