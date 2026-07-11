# EventHub OpenAPI contract instructions

## Scope

Applies to `contracts/**`. The committed source of public REST truth is `contracts/openapi/api.v1.yaml`.

## Contract-first rules

- Change the OpenAPI source before or together with runtime/client changes; do not treat generated output as authoritative.
- Keep operations, request/response schemas, status codes, authentication requirements, formats, nullability, and examples explicit.
- Model HTTP errors with RFC 7807 ProblemDetails and stable application error codes.
- Do not expose domain entities, persistence models, provider secrets, internal integration events, full ticket codes, or unnecessary personal data.
- Preserve transparent pricing: public event, checkout summary, and charged total must describe the same final amount.
- Preserve public versus protected operation boundaries from `features.md`.
- Avoid breaking changes. When a break is intentional, reconcile `features.md`, runtime mapping, generated consumers, and migration/rollout implications in the same change.

## Generated artifacts

- Do not hand-edit `web/src/generated/` or other generated OpenAPI output.
- Use the repository's existing generation/verification scripts.
- Review generated diffs for accidental schema widening, renamed fields, nullable changes, or removed responses.

## Verification

```powershell
yarn --cwd web api:verify
dotnet test EventHub.slnx -c Release
```
