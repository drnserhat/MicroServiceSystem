import { useMemo, useState } from "react";
import { PreviewBanner, SectionHeader } from "@/components/control";

type Generator = {
  id: string;
  title: string;
  description: string;
  command: string;
};

const GENERATORS: Generator[] = [
  {
    id: "service",
    title: "Create service",
    description: "Scaffold a new bounded context with Api/Application/Domain/Infrastructure/Persistence.",
    command:
      "dotnet new install ./templates/msf-service\ndotnet new msf-service -n Product -o src/Services/Product --db postgres --publishes-events true",
  },
  {
    id: "crud",
    title: "Create CRUD aggregate",
    description: "Generate aggregate, handlers, validators, EF config, and versioned controller.",
    command:
      "dotnet new install ./templates/msf-crud\ncd src/Services/Product\ndotnet new msf-crud -n Category --service Product --route categories --permission-prefix product -o .",
  },
  {
    id: "event",
    title: "Create integration event",
    description: "Add a contract under Shared/Contracts and publish via outbox (manual pattern).",
    command: "# Add event type under src/Shared/Contracts/Events\n# Publish with IIntegrationEventPublisher in the same UnitOfWork",
  },
  {
    id: "saga",
    title: "Create saga",
    description: "Orchestration lives in Coordinator — steps implement ISagaStep.",
    command: "# See Coordinator RegisterUserSaga + BuildingBlocks.Saga\n# Do not orchestrate inside random domain services",
  },
];

export function DeveloperPage() {
  const [selectedId, setSelectedId] = useState(GENERATORS[0]!.id);
  const [copied, setCopied] = useState(false);
  const selected = useMemo(() => GENERATORS.find((g) => g.id === selectedId) ?? GENERATORS[0]!, [selectedId]);

  async function copy() {
    await navigator.clipboard.writeText(selected.command);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <>
      <SectionHeader
        title="Developer Center"
        description="Framework developer tools — CLI templates only (no codegen backend)."
      />
      <PreviewBanner>
        Buttons do not execute generators remotely. Copy commands and run them in the repo.
      </PreviewBanner>

      <div className="row">
        <div className="col-md-4">
          <div className="list-group mb-3">
            {GENERATORS.map((gen) => (
              <button
                key={gen.id}
                type="button"
                className={`list-group-item list-group-item-action ${selectedId === gen.id ? "active" : ""}`}
                onClick={() => setSelectedId(gen.id)}
              >
                {gen.title}
              </button>
            ))}
          </div>
        </div>
        <div className="col-md-8">
          <div className="card">
            <div className="card-body">
              <h3 className="card-title">{selected.title}</h3>
              <p className="text-secondary">{selected.description}</p>
              <pre className="bg-dark text-light p-3 rounded" style={{ whiteSpace: "pre-wrap" }}>
                {selected.command}
              </pre>
              <button type="button" className="btn btn-primary" onClick={() => void copy()}>
                {copied ? "Copied" : "Copy commands"}
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
