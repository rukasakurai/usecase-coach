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

Run a thorough review by fanning out **nine subagents in parallel**, one per
perspective, then aggregating their findings into one prioritized report. Each
subagent examines the *same* changes through a *different* lens, so coverage is
broad while each lens stays deep and focused.

## 1. Determine the review scope and objective

1. Establish exactly what is under review and capture it as a concrete diff:
   PR → `gh pr diff <n>`; current branch → `git diff main...HEAD` (fall back to
   the repo's default branch); local work → `git diff` / `git diff --staged`.
2. Record the changed files + the diff. Pass this **identical** scope to every
   subagent.
3. Identify the **objective** the change must satisfy — the linked GitHub issue,
   the PR description, or the user's stated goal (e.g. `gh issue view <n>`),
   including any explicit out-of-scope items. The simplicity and scope-cutting
   perspectives require this.

## 2. Dispatch nine subagents in parallel

Launch all nine with the `task` tool, `agent_type: general-purpose`, in a single
batch so they run concurrently. For each subagent, read the matching rubric file
in this skill's `perspectives/` directory and include **its full contents** in
the subagent's prompt, together with the review scope, the objective, and the
shared output contract below.

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

## 3. Aggregate into one report

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
