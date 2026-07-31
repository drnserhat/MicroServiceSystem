import { Link, Outlet, useParams } from "react-router-dom";
import { CopyCommandBlock, HubTabs, PageFrame, PreviewBanner } from "@/components/control";
import { WIZARDS, findWizard } from "./catalog";

const TABS = [
  { to: "/developer", label: "Overview", end: true },
  ...WIZARDS.map((w) => ({ to: `/developer/${w.id}`, label: w.title })),
];

export function DeveloperLayout() {
  return (
    <PageFrame
      pretitle="Developer"
      title="Developer Center"
      description="Framework developer tools — CLI templates only (no codegen backend)."
      actions={
        <Link className="btn" to="/building-blocks">
          BuildingBlocks
        </Link>
      }
    >
      <HubTabs tabs={TABS} label="Developer wizards" />
      <Outlet />
    </PageFrame>
  );
}

export function DeveloperOverviewPage() {
  return (
    <>
      <PreviewBanner>
        Buttons do not execute generators remotely. Copy commands and run them in the repo.
      </PreviewBanner>
      <div className="row row-cards">
        {WIZARDS.map((wizard) => (
          <div className="col-md-6 col-xl-4" key={wizard.id}>
            <div className="card h-100">
              <div className="card-body">
                <h3 className="card-title">{wizard.title}</h3>
                <p className="text-secondary">{wizard.description}</p>
                <Link className="btn btn-sm" to={`/developer/${wizard.id}`}>
                  Open wizard
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
  const { wizardId } = useParams<{ wizardId: string }>();
  const wizard = wizardId ? findWizard(wizardId) : undefined;

  if (!wizard) {
    return (
      <div className="card">
        <div className="card-body">
          <p className="text-secondary mb-2">
            Unknown wizard <code>{wizardId}</code>.
          </p>
          <Link className="btn" to="/developer">
            Back
          </Link>
        </div>
      </div>
    );
  }

  return (
    <>
      <PreviewBanner>Copy-only UX — no remote scaffold execution.</PreviewBanner>
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
