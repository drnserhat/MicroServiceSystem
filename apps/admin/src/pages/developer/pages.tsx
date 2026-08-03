import { Link, Outlet, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { CopyCommandBlock, HubTabs, PageFrame, PreviewBanner } from "@/components/control";
import { WIZARDS, findWizard } from "./catalog";

export function DeveloperLayout() {
  const { t } = useTranslation(["developer", "architecture"]);
  const tabs = [
    { to: "/developer", label: t("overview"), end: true as const },
    ...WIZARDS.map((w) => ({ to: `/developer/${w.id}`, label: w.title })),
  ];

  return (
    <PageFrame
      pretitle={t("hubPretitle")}
      title={t("hubTitle")}
      description={t("hubDescription")}
      actions={
        <Link className="btn" to="/building-blocks">
          {t("architecture:buildingBlocks")}
        </Link>
      }
    >
      <HubTabs tabs={tabs} label={t("hubTitle")} />
      <Outlet />
    </PageFrame>
  );
}

export function DeveloperOverviewPage() {
  const { t } = useTranslation("developer");

  return (
    <>
      <PreviewBanner>{t("previewBannerOverview")}</PreviewBanner>
      <div className="row row-cards">
        {WIZARDS.map((wizard) => (
          <div className="col-md-6 col-xl-4" key={wizard.id}>
            <div className="card h-100">
              <div className="card-body">
                <h3 className="card-title">{wizard.title}</h3>
                <p className="text-secondary">{wizard.description}</p>
                <Link className="btn btn-sm" to={`/developer/${wizard.id}`}>
                  {t("openWizard")}
                </Link>
              </div>
            </div>
          </div>
        ))}
      </div>
    </>
  );
}

export function DeveloperWizardPage() {
  const { t } = useTranslation("developer");
  const { wizardId } = useParams<{ wizardId: string }>();
  const wizard = wizardId ? findWizard(wizardId) : undefined;

  if (!wizard) {
    return (
      <div className="card">
        <div className="card-body">
          <p className="text-secondary mb-2">
            {t("unknownWizard")} <code>{wizardId}</code>.
          </p>
          <Link className="btn" to="/developer">
            {t("back")}
          </Link>
        </div>
      </div>
    );
  }

  return (
    <>
      <PreviewBanner>{t("previewBannerWizard")}</PreviewBanner>
      <div className="card">
        <div className="card-body">
          <h3 className="card-title">{wizard.title}</h3>
          <p className="text-secondary">{wizard.description}</p>
          <CopyCommandBlock command={wizard.command} />
        </div>
      </div>
    </>
  );
}
