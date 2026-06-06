# Perspective: Azure Well-Architected Framework — Security

Review the changes for security of a .NET container on Azure Container Apps (ACA)
built/deployed via Dockerfile, Bicep, ACR, and GitHub Actions. Source: Microsoft
Learn WAF Security checklist (SE:05/08/09) and the ACA WAF service guide.

Look for:

- **Image pull identity** — Does the container app pull from ACR using a
  **managed identity + `AcrPull` RBAC**, or via **ACR admin credentials**
  (`adminUserEnabled: true` + `username`/`passwordSecretRef`)? Admin creds are
  long-lived and unaudited per-identity — flag them.
- **Secret handling** — Are sensitive env vars `secretRef` / Key Vault references,
  not plaintext `value:` in Bicep? Any secret, key, or token committed to source
  or visible in ARM/deploy output is Critical.
- **Ingress authentication** — Is `ingress.external: true` exposed with **no**
  `authConfig` (Entra Easy Auth) or app-layer auth? A public unauthenticated MCP
  endpoint is a finding (calibrate to whether the objective intends public access).
- **CI auth** — Does GitHub Actions use **OIDC federated identity**
  (`azure/login@v2` with `client-id`/`tenant-id`/`subscription-id`) rather than a
  long-lived `AZURE_CREDENTIALS` client secret?
- **Container hardening** — Does the Dockerfile run as **non-root** (`USER app`)?
  Does the final stage use a **minimal base image** (alpine/chiseled) and avoid
  shipping the SDK?
- **Supply-chain scanning** — Is there image vulnerability scanning in CI (Trivy,
  Defender for Containers) before deploy?
- **TLS** — Is `ingress.allowInsecure: false` (plain HTTP not permitted)?
- **Least privilege** — Does the app's identity hold only the roles it needs
  (e.g. read-only), not `Contributor` on the resource group?
