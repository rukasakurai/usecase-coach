---
name: multi-perspective-review
description: >-
  Reviews a set of code changes in parallel from nine independent perspectives —
  the five Azure Well-Architected Framework pillars (Reliability, Security, Cost
  Optimization, Operational Excellence, Performance Efficiency), the OWASP Top 10
  for LLM Applications, the OWASP Top 10 for Agentic Applications, repository
  simplicity (CONTRIBUTING.md / AGENTS.md), and aggressive scope-cutting against
  the stated objective. Use for a thorough multi-angle review of a PR, branch, or
  diff, or when asked for a "WAF / OWASP / well-architected / multi-perspective"
  review.
---

# Multi-Perspective Review

Run a thorough review from nine perspectives. Scale effort to the size of the
change: under GitHub's token-based billing every subagent model call is billed,
so subagents should only be launched when the diff is large enough to justify
them. Each perspective examines the *same* changes through a *different* lens.

## 1. Determine the review scope and objective

1. Establish exactly what is under review and capture it as a concrete diff:
   PR → `gh pr diff <n>`; current branch → `git diff main...HEAD` (fall back to
   the repo's default branch); local work → `git diff` / `git diff --staged`.
2. Record the changed files + the diff. Pass this **identical** scope to every
   subagent (if used).
3. Identify the **objective** the change must satisfy — the linked GitHub issue,
   the PR description, or the user's stated goal (e.g. `gh issue view <n>`),
   including any explicit out-of-scope items. The simplicity and scope-cutting
   perspectives require this.

## 2. Choose the review mode based on diff size

Run `git diff --stat` (or `gh pr diff <n> --stat`) and count total **changed lines**
and **changed files**.

| Tier | Signal | Action |
|------|--------|--------|
| **Tiny** | ≤ 50 changed lines **or** ≤ 3 files | Review all perspectives **inline** — read the relevant rubric files yourself. No subagents. |
| **Small** | 51–200 lines **and** 4–8 files | Launch **3–4 most relevant subagents** in parallel; cover the rest inline. |
| **Large** | > 200 lines **or** > 9 files | Launch all nine perspectives as **parallel subagents** (full fan-out). |

For Small diffs, choose the 3–4 perspectives most applicable to the change type
(e.g., a new auth flow → Security, OWASP-LLM, OWASP-Agentic, Simplicity; an
infra-only change → Reliability, Cost, Operational Excellence, Security).

## 3. Dispatch subagents (Small and Large tiers only)

Launch with the `task` tool, `agent_type: general-purpose`. For Large diffs,
launch all nine in a single batch so they run concurrently. For each subagent,
read the matching rubric file in this skill's `perspectives/` directory and include
**its full contents** in the subagent's prompt, together with the review scope, the
objective, and the shared output contract below. Use `model: claude-haiku-4.5` for
Small-tier subagents to reduce cost; reserve the default Sonnet model for Large-tier
full fan-outs.

| # | Perspective | Rubric file |
|---|-------------|-------------|
| 1 | WAF · Reliability | `perspectives/waf-reliability.md` |
| 2 | WAF · Security | `perspectives/waf-security.md` |
| 3 | WAF · Cost Optimization | `perspectives/waf-cost-optimization.md` |
| 4 | WAF · Operational Excellence | `perspectives/waf-operational-excellence.md` |
| 5 | WAF · Performance Efficiency | `perspectives/waf-performance-efficiency.md` |
| 6 | OWASP Top 10 for LLM Applications | `perspectives/owasp-llm-top-10.md` |
| 7 | OWASP Top 10 for Agentic Applications | `perspectives/owasp-agentic-top-10.md` |
| 8 | Repository simplicity | `perspectives/simplicity.md` |
| 9 | Aggressive scope-cutting | `perspectives/aggressive-cutting.md` |

Once a perspective is delegated, let its subagent own it — do not review it
yourself. If a subagent returns nothing useful, do that one perspective yourself
using its rubric file.

### Shared output contract (give to every subagent)

> **Tool call budget: stop after at most 15 tool calls.** Start with the diff
> itself; read surrounding files only when essential to form a concrete finding.
> If you are uncertain without more exploration, note the uncertainty and stop —
> do not keep digging.
>
> Report only genuine, actionable findings — no style nits, no praise, no filler.
> For each finding output:
> - **Severity**: Critical / High / Medium / Low (definitions below)
> - **Location**: `path:line` (or the resource/section)
> - **Finding**: what is wrong, concretely
> - **Why it matters**: the concrete risk or cost, tied to this perspective
> - **Suggested fix**: the smallest change that resolves it
>
> Prefer 3 real issues over 15 speculative ones. Quote exact file/line evidence.
> If your perspective's best practice would conflict with the repository's
> "default to less" principle, still report the gap but say so and calibrate
> severity to the project's stage and objective. If you find nothing, say so.

**Severity definitions** (shared):
- **Critical** — exploitable security hole, data loss, or a broken/blocked deploy.
- **High** — likely production incident, real vulnerability, or clear objective miss.
- **Medium** — meaningful risk, cost, or maintainability problem; should fix.
- **Low** — minor improvement; safe to defer.

## 4. Aggregate into one report

1. **Merge & de-duplicate** — the same issue often surfaces under several lenses;
   keep one entry and note the lenses it spans.
2. **Sort** by severity, then perspective.
3. **Apply the "default to less" reconciliation pass** — when perspectives
   conflict (e.g. "add zone redundancy / App Insights / canary" vs. "cut
   everything non-essential"), state the trade-off and recommend the option
   consistent with the objective and the repo's minimalism. An early concept-stage
   repo should not be told to adopt every enterprise best practice.
4. Output one tight Markdown report:
   - a one-line **verdict** (ship / fix-then-ship / needs-work),
   - findings grouped by severity,
   - the perspectives that found nothing (for confidence),
   - a short **"safe to cut"** list from the scope-cutting perspective.

The goal is signal, not volume.
