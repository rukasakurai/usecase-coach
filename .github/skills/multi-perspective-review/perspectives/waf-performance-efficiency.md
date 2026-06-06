# Perspective: Azure Well-Architected Framework — Performance Efficiency

Review the changes for performance of a .NET container on ACA. Source: Microsoft
Learn WAF Performance Efficiency checklist (PE:01/05/06/07) and the ACA WAF
service guide.

Look for:

- **Performance targets** — Are numeric targets (e.g. p99 latency, throughput,
  max acceptable cold-start) documented anywhere? Without targets, config choices
  can't be judged.
- **Validated scale rule** — Is the HTTP scale rule (`concurrentRequests`)
  derived from load testing, or just the platform default of 10?
- **Image / startup optimization** — Does the Dockerfile use a multi-stage build,
  publish in **Release**, and consider ReadyToRun for faster JIT/startup? Is the
  final image lean (no SDK/test assets)?
- **Startup probe tuning** — Is the startup probe's `failureThreshold` matched to
  measured cold-start time so the app isn't killed prematurely or kept unhealthy
  too long?
- **maxReplicas** — Is there a deliberate ceiling so scale-out stays controlled
  under load?
- **Hot-path efficiency** — In the app code, are there avoidable per-request costs
  (e.g. re-reading/parsing files on every call when they could be cached) relative
  to the stated targets?

Calibrate: formal load testing and SLOs may be premature for a concept repo, but
cheap wins (Release build, multi-stage image, avoiding per-call re-parsing) are
worth flagging.
