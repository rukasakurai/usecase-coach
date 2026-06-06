# Angle: Azure cloud architect (security posture)

Review the change as a senior Azure architect responsible for the workload's
**cloud security posture**. The stack is Azure Container Apps (ACA) + Azure
Container Registry + Log Analytics, provisioned with Bicep + azd, deployed by
GitHub Actions. Anchor on the Azure Well-Architected Framework **Security** pillar
and ACA security guidance; cover the most consequential controls first.

Assess these areas in the changes:

- **Workload identity & image pull** — Does the container app use a **managed
  identity + `AcrPull` RBAC** to pull images, or **ACR admin credentials**
  (`adminUserEnabled: true`)? Admin creds are long-lived, shared, and unaudited —
  prefer managed identity. Flag the trade-off if RBAC was avoided for convenience.
- **Secrets management** — Sensitive values as Container Apps secrets /
  `secretRef` / Key Vault references with managed identity, not plaintext env
  `value:` in Bicep. No secrets in ARM/deploy outputs.
- **Network exposure** — Is `ingress.external: true` necessary, or could it be
  internal-only? Is authentication enforced at ingress (Entra Easy Auth) for a
  public endpoint? Is `allowInsecure: false` (HTTPS only)?
- **Least privilege (RBAC)** — Does the workload identity and the **CI deployment
  principal** hold only the roles they need (e.g. scoped `AcrPush`/`Contributor`
  on a resource group vs. subscription-wide), following just-enough-access?
- **CI/CD trust** — GitHub Actions authenticating via **OIDC federated identity**
  (`azure/login@v2`) rather than a stored client secret? Federated credential
  subject scoped to the intended repo/branch/PR? Actions pinned?
- **IaC hygiene** — Bicep parameterized (no hardcoded environment-specific IDs),
  resources tagged, deployments reproducible. No drift-inducing manual steps.
- **Data protection & observability** — Diagnostic/audit logging to Log Analytics
  enabled; tool-call auditing possible; encryption in transit enforced. Are there
  alerts for security-relevant events?
- **Container hardening** — Non-root user, minimal/pinned base image, image
  vulnerability scanning before deploy.

For each finding, state the **control gap**, the Azure-specific remediation (the
concrete Bicep/identity/role change), and the residual risk if not fixed.
Calibrate to the project's stage — flag enterprise controls that are reasonable to
defer, but always call out identity, secrets, and public-exposure gaps.
