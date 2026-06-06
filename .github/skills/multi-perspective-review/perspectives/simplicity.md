# Perspective: Repository simplicity (CONTRIBUTING.md / AGENTS.md)

The repository's canonical working principle is **"Default to less. Removing or
refactoring is usually better than adding."** (AGENTS.md, CONTRIBUTING.md). Review
the changes against the explicit `CONTRIBUTING.md` rules and flag violations:

- **Start lean** — Is this the minimal, focused implementation, or a comprehensive
  solution where a smaller one would do? Flag speculative abstraction,
  configurability, or layers added "for the future."
- **Minimize changes** — Are the modifications the smallest that achieve the goal?
  Flag incidental refactors, churn, or reformatting bundled into the change.
- **Defer specifics** — Flag any hardcoded environment-specific value (tenant /
  subscription / client IDs, region, resource names, secrets) that should be a
  parameter or env var.
- **Public-safe by default** — Flag any secret, key, token, or sensitive Azure
  identifier committed to source.
- **Respect deferred decisions** — Flag licenses, deployment specifics, or
  technology scaffolding added without being explicitly requested.
- **New dependencies / files** — For each new package, file, or tool, ask: is it
  necessary, or can existing code cover it? Flag additions that don't earn their
  keep.
- **Comments & docs** — Flag commentary that restates the code rather than
  clarifying non-obvious intent.

Bias toward recommending **removal or simplification**. If the change is already
minimal, say so plainly.
