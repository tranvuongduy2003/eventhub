# Backend Source Map

Use this as a source-discovery guide, not a rule document. `AGENTS.md` and `docs/technical.md` own architecture rules.

## Start From The Slice

For a behavior change, trace the vertical slice before editing:

```text
Api endpoint or contract
-> Application command/query and handler
-> Domain aggregate/entity/value object behavior
-> Infrastructure repository/adapter and EF mapping
-> Domain.UnitTests or Api.IntegrationTests coverage
```

If a layer is not involved, skip it.

## Discovery Commands

```powershell
rg -n "sealed record .*Command|sealed record .*Query" src/Application
rg -n "class .*Handler|CommandHandler<|QueryHandler<" src/Application
rg -n "IPipelineBehavior|AddTransient\(typeof\(IPipelineBehavior" src/Application
rg -n "interface I.*Repository|class .*Repository" src/Application src/Infrastructure
rg -n "class .*Endpoint|IEndpoint" src/Api
```

Open the matching registration file when behavior depends on pipeline, DI, endpoint discovery, options, or hosted services.

## Drift Checks

Before preserving old behavior, compare it against:

- relevant `ARCH-*`, `INV-*`, and bounded-context sections in `docs/technical.md`;
- relevant `F-*` acceptance criteria in `docs/features.md`;
- the scoped `AGENTS.md` for the layer being edited;
- current code registrations and tests.

Report drift instead of encoding a second copy of the rule here.
