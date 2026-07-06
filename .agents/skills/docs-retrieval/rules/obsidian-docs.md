# Obsidian Docs Retrieval Rules

Use these rules with `docs-retrieval` when precision matters.

## Do

- Enter through `docs/README.md`, `docs/_memory/source-of-truth-map.md`, or the task-specific MOC.
- Prefer Obsidian links as navigation hints, then read the linked source file directly.
- Search stable IDs first: `DEC-*`, `QG-*`, `EP-*`, `F-*`, `BC-*`, `AGG-*`, `INV-*`, `EVT-*`.
- Keep answers tied to the highest-precedence document that supports them.
- Mention unresolved drift when lower-level docs contradict source memory.

## Do Not

- Do not treat MOCs or glossaries as replacements for source memory.
- Do not load all source docs by default.
- Do not treat old plans, local notes, or ignored state as source of truth.
- Do not silently resolve conflicts by averaging documents.
- Do not update derived memory without checking whether a source document owns the fact.
