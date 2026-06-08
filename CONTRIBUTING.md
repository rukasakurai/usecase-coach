# Contributing

Thank you for your interest in contributing to this project. This document is the canonical source of contribution guidelines and applies to **all contributors** — whether working manually or through automated tooling.

## Working principle

**Default to less. Removing or refactoring is usually better than adding.**

## Collaboration & decision-making style

- **Start lean**: prefer minimal, focused implementations over comprehensive solutions.
- **Minimize changes**: make the smallest possible modifications to achieve the goal.
- **Defer specifics**: avoid hardcoding environment-specific values (tenant IDs, subscription IDs, secrets).
- **Public-safe by default**: never suggest or add sensitive information (secrets, Azure IDs, API keys).
- **Respect deferred decisions**: do not add licenses, specific deployment configurations, or technology-specific scaffolding unless explicitly requested.

## Drafting issues

To turn a discussion or code analysis into a lean, problem-focused GitHub issue, use the reusable [`draft-github-issue`](https://github.com/rukasakurai/agent-skills/blob/main/.github/skills/draft-github-issue/SKILL.md) agent skill rather than maintaining an equivalent skill in this repository:

```bash
gh skill install rukasakurai/agent-skills draft-github-issue
```

Requires GitHub CLI v2.90.0+. See the [agent-skills README](https://github.com/rukasakurai/agent-skills) for install scopes, pinning, and updates.
