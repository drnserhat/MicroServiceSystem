export type Wizard = {
  id: string;
  title: string;
  description: string;
  command: string;
};

export const WIZARDS: Wizard[] = [
  {
    id: "service",
    title: "Create service",
    description: "Scaffold a new bounded context with Api/Application/Domain/Infrastructure/Persistence.",
    command:
      "dotnet new install ./templates/msf-service\ndotnet new msf-service -n Product -o src/Services/Product --db postgres --publishes-events true",
  },
  {
    id: "aggregate",
    title: "Create aggregate",
    description: "Domain aggregate root + value objects following SharedKernel primitives.",
    command:
      "# Under Domain/\n# public sealed class Product : AggregateRoot<Guid>\n# Add invariants; raise domain events; map in Persistence",
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
    command:
      "# Add event type under src/Shared/Contracts/Events\n# Publish with IIntegrationEventPublisher in the same UnitOfWork",
  },
  {
    id: "saga",
    title: "Create saga",
    description: "Orchestration lives in Coordinator — steps implement ISagaStep.",
    command:
      "# See Coordinator RegisterUserSaga + BuildingBlocks.Saga\n# Do not orchestrate inside random domain services",
  },
  {
    id: "building-block",
    title: "Create building block",
    description: "Add a cross-cutting package under BuildingBlocks — prefer extending existing blocks.",
    command:
      "# Prefer extending BuildingBlocks.* over new packages\n# Wire via ServiceDefaults / DI extensions\n# Document in admin BuildingBlocks catalog",
  },
  {
    id: "templates",
    title: "Templates",
    description: "List installed MSF dotnet new templates and refresh from ./templates.",
    command: "dotnet new list msf\ndotnet new install ./templates/msf-service --force\ndotnet new install ./templates/msf-crud --force",
  },
];

export function findWizard(id: string): Wizard | undefined {
  return WIZARDS.find((w) => w.id === id);
}
