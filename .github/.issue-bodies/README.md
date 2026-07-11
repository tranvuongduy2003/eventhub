# Issue Body Template

Skeleton for `gh issue create --body-file` when creating a feature-spec tracking issue.

Use one issue per spec; do not split one feature spec into epic plus per-story issues.

| File | Use |
| --- | --- |
| `spec.template.md` | Single tracking issue for the whole feature spec |

## Workflow

1. Copy `spec.template.md` to a temp path.
2. Fill from `docs/specs/<YYYYMMDDHHmmss>-<feature>.md` with problem, acceptance criteria, and links.
3. Run `gh issue create --title "Spec: ..." --label "spec,enhancement" --body-file <path>` when GitHub work is explicitly requested.
4. If the spec tracks GitHub metadata, record the issue number in the spec.
