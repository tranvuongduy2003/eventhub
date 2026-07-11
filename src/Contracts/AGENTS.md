# EventHub Contracts instructions

## Scope

Applies to `src/Contracts/**`. Inherits `src/AGENTS.md` and the root instructions. Read `contracts/AGENTS.md` for public REST shape changes.

## Contract rules

- Keep request and response types explicit, transport-focused, and free of domain behavior.
- Do not expose domain entities, EF models, provider payloads, internal event envelopes, secrets, or infrastructure-specific types.
- Keep names, nullability, formats, validation meaning, and enum values aligned with `contracts/openapi/api.v1.yaml`.
- Prefer stable, consumer-oriented shapes over leaking internal aggregate structure.
- Preserve backward compatibility unless the relevant specification deliberately changes the public behavior.
- Keep ProblemDetails codes and semantics stable and intentional.
- A contract DTO is not the source of business truth; domain invariants remain in Domain and observable behavior remains in `features.md`.

## Change completion

A public contract change is incomplete until the OpenAPI source, endpoint mapping, generated web types, affected frontend calls, and integration tests agree.

## Verification

```powershell
yarn --cwd web api:verify
dotnet build EventHub.slnx -c Release
dotnet test EventHub.slnx -c Release
```
