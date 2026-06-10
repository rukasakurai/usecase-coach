---
name: security-review
description: >-
  Security-focused review of code changes run in parallel from three adversarial
  and architectural angles — a white-hat hacker probing for exploitable
  vulnerabilities, a confidentiality reviewer hunting for leaked secrets and
  sensitive-data exposure, and an Azure cloud architect assessing cloud security
  posture (identity, secrets, network, least privilege). Use when asked for a
  security review, threat assessment, "can this be hacked", secrets/leak check, or
  a cloud security posture review of a PR, branch, or diff.
---

# Security Review

Run a focused security review from three angles — offensive (break in),
confidentiality (get data out), and architectural (cloud posture). Scale effort
to the size of the change: subagents are expensive and should only be launched
when the diff is large enough to justify them.

## 1. Determine the review scope and objective

1. Capture exactly what is under review as a concrete diff: PR →
   `gh pr diff <n>`; current branch → `git diff main...HEAD` (fall back to the
   default branch); local work → `git diff` / `git diff --staged`.
2. Record the changed files + diff and pass the **identical** scope to every
   subagent (if used). Also give them read access to the surrounding code they
   need to judge exploitability (auth middleware, IaC, workflows, data files).
3. Note the system context: this repo's MCP server returns JSON records from disk
   to LLM clients over Streamable HTTP, deployed to Azure Container Apps via Bicep
   + azd + GitHub Actions (OIDC), with optional Entra auth.

## 2. Choose the review mode based on diff size

Run `git diff --stat` (or `gh pr diff <n> --stat`) and count total **changed lines**
and **changed files**.

| Tier | Signal | Action |
|------|--------|--------|
| **Tiny** | ≤ 50 changed lines **or** ≤ 3 files | Review all three angles **inline** — read the rubric files yourself and apply each lens directly. No subagents. |
| **Small** | 51–200 lines **and** 4–8 files | Launch **only the most relevant angle** as a single subagent; do the other two inline. |
| **Large** | > 200 lines **or** > 9 files | Launch all three angles as **parallel subagents** (original full fan-out). |

For Small diffs, pick the single most security-relevant angle for the change type
(e.g., IaC/auth change → cloud architect; new endpoint → white-hat hacker; secrets
or data handling → confidential leakage).

## 3. Dispatch subagents (Small and Large tiers only)

Launch with the `task` tool, `agent_type: general-purpose`. For each subagent,
read the matching rubric file in this skill's `perspectives/` directory and include
**its full contents** in the subagent's prompt, along with the scope and the shared
output contract below.

| # | Angle | Rubric file |
|---|-------|-------------|
| 1 | White-hat hacker (offensive) | `perspectives/white-hat-hacker.md` |
| 2 | Confidential information leakage | `perspectives/confidential-information-leakage.md` |
| 3 | Azure cloud architect (posture) | `perspectives/azure-cloud-architect.md` |

Once an angle is delegated, let its subagent own it. If a subagent returns nothing
useful, do that angle yourself using its rubric file.

### Shared output contract (give to every subagent)

> **Tool call budget: stop after at most 15 tool calls.** Start with the diff
> itself; read surrounding files only when essential to judge exploitability. If
> you are uncertain whether a path is exploitable without more exploration, note
> the uncertainty and stop — do not keep digging.
>
> Report only genuine, actionable security findings — no style nits, no praise,
> no speculative "could theoretically" filler. For each finding output:
> - **Severity**: Critical / High / Medium / Low (definitions below)
> - **Location**: `path:line` (or resource/section)
> - **Finding**: the vulnerability or exposure, concretely
> - **Attack / impact**: how it is exploited and what it costs (be specific)
> - **Fix**: the smallest change that closes it
>
> Prefer a few real, demonstrable issues over many hypotheticals. Quote exact
> file/line evidence. If you find nothing in your angle, say so explicitly.

**Severity definitions** (shared):
- **Critical** — remotely exploitable, or a leaked live secret / sensitive data set.
- **High** — likely exploitable vulnerability or a real exposure path.
- **Medium** — meaningful weakness needing defense-in-depth; not directly exploitable.
- **Low** — minor hardening opportunity.

## 4. Aggregate into one report

1. **Merge & de-duplicate** — the same issue often appears under multiple angles
   (e.g. ACR admin credentials = hacker *and* architect *and* leakage). Keep one
   entry and note the angles that flagged it; agreement raises confidence.
2. **Sort** by severity.
3. Output one tight Markdown report:
   - a one-line **verdict** (safe to ship / fix-then-ship / blocker present),
   - findings grouped by severity with the fix for each,
   - a short list of **verified-clean** areas (what each angle checked and found
     sound), for confidence.

The goal is exploitable signal, not a checklist dump. Do not invent vulnerabilities.
