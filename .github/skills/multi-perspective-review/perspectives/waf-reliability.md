# Perspective: Azure Well-Architected Framework — Reliability

Review the changes for reliability of a containerized .NET HTTP service deployed
to Azure Container Apps (ACA) via Bicep + azd. Source: Microsoft Learn WAF
Reliability checklist and the ACA WAF service guide.

Look for:

- **Health probes** — Are explicit liveness / readiness / startup HTTP probes
  defined on the container (port 8080), or is it relying on the ACA default TCP
  probe? A .NET app with DI warm-up can hit restart loops without a tuned startup
  probe.
- **Cold start vs. scale-to-zero** — `minReplicas: 0` with synchronous LLM/MCP
  callers risks breaching caller timeouts on cold start. Expect either
  `minReplicas: 1` or a documented, accepted cold-start tolerance.
- **Resource limits** — Are explicit `cpu`/`memory` set on the container, to
  avoid noisy-neighbor contention on the Consumption plan?
- **Scale rules** — Is there an explicit HTTP scale rule (`concurrentRequests`)
  and a deliberate `maxReplicas`, rather than just platform defaults?
- **Zone redundancy** — Where the environment type/region supports it, is
  `zoneRedundant: true` set on the managed environment? (Note: not available on
  every plan/region — flag as "where available".)
- **Failure handling** — For any external dependency or startup work, are there
  timeouts/retries so a slow dependency doesn't wedge the app?
- **Single points of failure** — Any new resource that becomes a hard dependency
  with no redundancy or graceful degradation?

Calibrate: for an early concept-stage repo, zone redundancy and multi-replica
HA may be deliberately deferred — report the gap but weigh severity accordingly.
