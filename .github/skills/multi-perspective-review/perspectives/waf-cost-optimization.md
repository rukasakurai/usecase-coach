# Perspective: Azure Well-Architected Framework — Cost Optimization

Review the changes for cost efficiency of an ACA workload provisioned via Bicep.
Source: Microsoft Learn WAF Cost Optimization checklist (CO:03/05/07/10/12) and
the ACA WAF service guide.

Look for:

- **Resource tags** — Do all Bicep resources (container app, managed environment,
  ACR, Log Analytics) carry cost-attribution tags (e.g. `env`, `workload`,
  `owner`)? Missing tags prevent spend tracking.
- **Scale-to-zero for idle workloads** — For a low-traffic server, is
  `minReplicas: 0` used (no charge at zero), or is an always-on replica justified?
- **Right-sized compute** — Are `cpu`/`memory` requests sized to actual need
  rather than over-provisioned without evidence?
- **ACR SKU** — Is the registry on an appropriate SKU (`Basic`/`Standard` for a
  small single-image project) rather than `Premium` without needing geo-replication
  / private link?
- **Log retention** — Is Log Analytics `retentionInDays` set deliberately rather
  than defaulting, so you neither overpay for retention nor lose needed logs?
- **maxReplicas ceiling** — Is there a deliberate `maxReplicas` so a traffic spike
  or runaway scaler can't produce a surprise bill?

Calibrate: tags and scale-to-zero are cheap wins worth flagging even for a small
repo; deeper cost tooling (budgets, policies) may be premature.
