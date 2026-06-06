# Perspective: Azure Well-Architected Framework — Operational Excellence

Review the changes for operability of an ACA workload deployed via Bicep + azd +
GitHub Actions. Source: Microsoft Learn WAF Operational Excellence checklist
(OE:05/06/07/11) and the ACA WAF service guide.

Look for:

- **Image tagging** — Is the deployed image tagged with the **commit SHA** (or a
  digest), not `latest`/a static tag? Stable tags make revision rollback
  impossible.
- **IaC parameterization** — Are environment-specific values (`env` name, region,
  ACR login server, image tag) declared as Bicep `param`s with `@description`,
  not hardcoded literals?
- **Observability** — Is the app instrumented (Application Insights /
  OpenTelemetry → Azure Monitor), or does it emit only stdout text? Is the managed
  environment wired to Log Analytics?
- **Alerting** — Are there alert rules on container restarts and HTTP 5xx rate so
  incidents surface proactively?
- **Safe deployment** — Is there a revision/traffic-split (canary/blue-green)
  strategy and a rollback path on failed health, rather than direct 100% cutover?
- **CI gates** — Does CI run tests/smoke checks before deploying, and are
  production deploys gated (environment protection / required reviewers)?

Calibrate: for a concept repo, App Insights, canary, and environment protection
may be deliberately deferred — flag the gaps but weigh severity against the goal.
