# Perspective: Aggressive scope-cutting vs. the objective

Goal: find everything that can be **cut** while still fully satisfying the stated
objective (the linked issue / PR goal). Be ruthless but correct — never cut
something the objective requires.

- **Restate the objective** in one sentence from the issue/PR, including its
  explicit **out-of-scope** items. Everything is judged against this.
- For **each** added file, resource, parameter, dependency, config block, and code
  path, ask: *"If I delete this, does the objective still hold and do the
  tests/deploy still pass?"* If yes → propose cutting it.
- Flag **gold-plating**: error handling for impossible cases, configurability no
  one asked for, premature optimization, defensive code without a real threat,
  retries/timeouts/caching not required by the goal.
- Flag **scope creep**: anything implementing an out-of-scope or future feature.
- Flag **redundancy**: duplicate logic, parallel mechanisms doing the same job,
  belt-and-suspenders checks, or infra that overlaps existing capabilities.
- Flag **unused surface**: parameters, outputs, env vars, branches, or public
  members nothing consumes.
- For every proposal, give the concrete **deletion** and a one-line argument for
  why the objective and its verification still hold. Group into **"safe to cut
  now"** vs. **"cut unless justified"**.

Output a prioritized cut-list (largest simplification first). If nothing can be
safely cut, state that the change is already minimal and why.
