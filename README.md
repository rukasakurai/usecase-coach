# usecase-coach

A Socratic discovery coach for finding AI use cases from one's own strengths and pain-points.

## What This Is

usecase-coach challenges copy-others thinking and judges ideas by the impact they would produce, rather than serving up generic examples to imitate. Reference use cases in this repo are inspiration for analogy, not templates to copy.

**Status: very early — concept stage.** No working coach yet.

## Repository Contents

- [`data/reference-usecases/`](data/reference-usecases/) — curated example AI use cases the coach draws on for analogy. Each `*.json` record validates against [`data/reference-usecases/schema.json`](data/reference-usecases/schema.json). See the directory [README](data/reference-usecases/README.md) for the field definitions.
- [`app/`](app/) — a .NET 10 MCP server that exposes the reference use cases as a tool (see [MCP Server](#mcp-server)).
- [`infra/`](infra/) — Bicep for deploying the MCP server to Azure Container Apps via `azd`.
- [`docs/`](docs/) — operational guides (Azure OIDC setup, Copilot coding agent guidance).
- [`CONTRIBUTING.md`](CONTRIBUTING.md) and [`AGENTS.md`](AGENTS.md) — collaboration guidelines for human and AI contributors.

## MCP Server

A minimal [Model Context Protocol](https://modelcontextprotocol.io/) server (in [`app/`](app/)) lets external AI clients query the reference use cases as a tool.

- **Runtime**: .NET 10 ASP.NET Core using [`ModelContextProtocol.AspNetCore`](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore) (Streamable HTTP transport).
- **Endpoint**: `/mcp` (a plain `GET /` returns a human-readable status string).
- **Tool**: `get_reference_usecases` — returns every record in `data/reference-usecases/` (excluding `schema.json`). The data directory is resolved from the `REFERENCE_USECASES_DIR` environment variable, falling back to a `data/reference-usecases` folder discovered upward from the working directory.

### Run locally

```bash
dotnet run --project app
# then connect an MCP client to http://localhost:<port>/mcp
```

### Deploy to Azure

The server deploys to Azure Container Apps with the Azure Developer CLI (`azd`), reusing the OIDC setup in [docs/azure-oidc-setup.md](docs/azure-oidc-setup.md):

```bash
azd up
```

`azd` builds the image from [`app/Dockerfile`](app/Dockerfile) (build context is the repo root so the use-case data is bundled) and provisions the resources in [`infra/`](infra/). After deployment, connect your MCP client to `https://<app-fqdn>/mcp` (the FQDN is shown in the `azd` output and as the `SERVICE_MCP_URI` output).

### Authentication (optional)

By default the endpoint is deployed without authentication. To enable Container Apps' built-in Microsoft Entra ID auth, register an Entra application and provide its values before deploying — no IDs are hardcoded:

```bash
azd env set ENTRA_CLIENT_ID <application-client-id>
azd env set ENTRA_OPENID_ISSUER https://login.microsoftonline.com/<tenant-id>/v2.0
```

When `ENTRA_CLIENT_ID` is set, the deployment adds an auth config that returns `401` to unauthenticated callers (suited to non-interactive MCP clients).

## Included Workflows

### Build Check

A fast pull-request check that compiles the MCP server and validates the Bicep — no Azure credentials required. Use it for quick feedback before the slower E2E deployment.

- **Trigger**: Pull requests that modify `app/` or `infra/`, or manual (`workflow_dispatch`)
- **File**: `.github/workflows/build-check.yml`
- **Steps**: `dotnet build` of `app/` and `az bicep build` of `infra/main.bicep`

### Azure OIDC Connectivity Check

A manual workflow that validates your Azure OIDC configuration is working correctly. Run it after completing the setup described in [docs/azure-oidc-setup.md](docs/azure-oidc-setup.md).

- **Trigger**: Manual (`workflow_dispatch`)
- **File**: `.github/workflows/azure-oidc-check.yml`

### E2E Test

Automates provisioning of infrastructure, application deployment, test execution, and cleanup using the Azure Developer CLI (`azd`). Creates a resource group (with optional tagging from repository secrets) before running `azd provision` and `azd deploy`.

- **Trigger**: Manual (`workflow_dispatch`) or on pull requests to `main` that modify `app/`, `infra/`, or `azure.yaml`
- **File**: `.github/workflows/e2e-test.yml`
- **Inputs** (manual trigger):
  - `cleanup` — Run `azd down` after tests (default: `true`)
  - `environment` — azd environment name (default: auto-generated from run ID)
  - `location` — Azure region (default: `japaneast`)

**Smoke test**: After deployment, the "Smoke test deployed MCP server" step calls the live endpoint (MCP `initialize` → `tools/list` → `tools/call`) and fails if `get_reference_usecases` is missing or returns no use cases. Extend that step with project-specific checks as needed.

#### Required Configuration

| Name | Type | Description |
|------|------|-------------|
| `AZURE_CLIENT_ID` | Variable | Application (client) ID |
| `AZURE_TENANT_ID` | Secret | Microsoft Entra tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Secret | Azure Subscription ID |
| `RG_TAG_NAME` | Secret | (Optional) Tag name for the resource group |
| `RG_TAG_VALUE` | Secret | (Optional) Tag value for the resource group |

### AZD Manage (Up / Down)

Enables on-demand creation (`azd up`) and destruction (`azd down`) of Azure environments using the Azure Developer CLI. Creates a resource group (with optional tagging from repository secrets) before running `azd up`. Useful for managing isolated development, test, or preview environments.

- **Trigger**: Manual (`workflow_dispatch`)
- **File**: `.github/workflows/azd-manage.yml`
- **Inputs**:
  - `action` — `up` or `down` (required)
  - `environment` — azd environment name (default: repository name, e.g. `my-project` → resource group `rg-my-project`)
  - `location` — Azure region (default: `japaneast`)

#### Usage

1. Go to **Actions** → **AZD Manage (Up / Down)**
2. Click **Run workflow**
3. Select `up` to provision or `down` to tear down
4. Optionally set a custom environment name and Azure region
