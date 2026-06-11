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
ASPNETCORE_HTTP_PORTS=5099 dotnet run --project app
# server is then available at http://localhost:5099/mcp
```

### Deploy to Azure

The server deploys to Azure Container Apps with the Azure Developer CLI (`azd`), reusing the OIDC setup in [docs/azure-oidc-setup.md](docs/azure-oidc-setup.md):

```bash
azd up
```

`azd` builds the image from [`app/Dockerfile`](app/Dockerfile) (build context is the repo root so the use-case data is bundled) and provisions the resources in [`infra/`](infra/). After deployment, connect your MCP client to `https://<app-fqdn>/mcp` (the FQDN is shown in the `azd` output and as the `SERVICE_MCP_URI` output).

### Test with a prompt

To exercise the server the way an AI agent would, register it with an MCP client and ask a natural-language question — the model decides to call `get_reference_usecases`. The steps are identical for a local or Azure deployment; only the URL differs:

- **Local**: `http://localhost:5099/mcp`
- **Azure**: `https://<app-fqdn>/mcp` (the `SERVICE_MCP_URI` from the `azd` output)

Using [GitHub Copilot CLI](https://docs.github.com/copilot/how-tos/use-copilot-agents/use-copilot-cli) as the client, run `/mcp add` and set the form fields as follows (press <kbd>Ctrl</kbd>+<kbd>S</kbd> to save):

- **Server Name**: `usecase-coach`
- **Server Type**: `2` (**HTTP**)
- **URL**: `http://localhost:5099/mcp` (local) or `https://<app-fqdn>/mcp` (Azure)
- **HTTP Headers**: leave empty
- **Tools**: `*` (or keep the default)

Equivalently, add it to `~/.copilot/mcp-config.json`:

```json
{
  "mcpServers": {
    "usecase-coach": {
      "type": "http",
      "url": "http://localhost:5099/mcp"
    }
  }
}
```

Then enter a prompt, for example:

> Using the usecase-coach server, show me a reference AI use case and explain the pattern.

#### Remove client setup after testing

- **GitHub Copilot CLI**: remove the `usecase-coach` server entry from your MCP config (either via `/mcp` in the CLI UI, or by deleting the `usecase-coach` object from `~/.copilot/mcp-config.json`).
- **VS Code (agent mode)**: remove the same `usecase-coach` MCP server entry from whichever scope you added it (User or Workspace settings).

**Other clients** accept the same URL — e.g. VS Code agent mode or Claude Desktop. For a quick check without an LLM, use the [MCP Inspector](https://github.com/modelcontextprotocol/inspector): `npx @modelcontextprotocol/inspector`, connect to the URL, and call `get_reference_usecases` directly.

> The default Azure deployment requires authentication: the client must present a valid Microsoft Entra token (see [Authentication](#authentication)). A deployment is only public if it explicitly opts out of auth.

### Authentication

The Azure deployment is **protected by Microsoft Entra ID by default**. The MCP server validates Microsoft Entra access tokens and advertises [RFC 9728](https://datatracker.ietf.org/doc/rfc9728/) OAuth 2.0 Protected Resource Metadata, as required by the [MCP authorization spec](https://modelcontextprotocol.io/specification/2025-06-18/basic/authorization). Unauthenticated calls to `/mcp` get a `401` whose `WWW-Authenticate` header points to `/.well-known/oauth-protected-resource`, so a spec-compliant MCP client can discover where to sign in. `azd up` provisions the endpoint's own Entra app registration (exposing a `user_impersonation` scope) — no app registration is created by hand and no client/tenant IDs are copied into configuration.

MCP clients that implement the authorization flow run an interactive OAuth 2.1 sign-in (browser, with automatic token refresh) — you do **not** paste tokens by hand:

- **VS Code** (agent mode): set `"oauth": { "clientId": "<MCP_ENTRA_CLIENT_ID>" }` on the server entry in `mcp.json`; VS Code opens a browser on first connection.
- **GitHub Copilot CLI**: configure the remote server with its `oauthClientId` (the OAuth flow then runs automatically).

The deployment exposes the values clients need as outputs: `azd env get-value MCP_ENTRA_CLIENT_ID` and `azd env get-value MCP_ENTRA_SCOPE`.

> **Note**: Authentication is enforced **in the server** (not Container Apps' built-in "Easy Auth"), because Easy Auth currently returns a bare `401` without the RFC 9728 resource-metadata pointer MCP clients need. If Easy Auth ships RFC 9728 support (the App Service [`WEBSITE_AUTH_PRM_DEFAULT_WITH_SCOPES`](https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-mcp) preview), this could move back to the platform.

Because the default deployment creates directory objects (an app registration and service principal), the identity running `azd up` needs permission to do so — for example the **Application Administrator** (or Cloud Application Administrator) Microsoft Entra role, or a tenant where the *Users can register applications* setting is enabled.

#### Deploy without authentication (opt-in)

Where the deploying identity cannot create directory objects (for example CI, which is why the [E2E Test](#e2e-test) workflow uses this path), opt out to deploy a public endpoint:

```bash
azd env set AUTH_DISABLED true
azd up
```

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

- **Trigger**: Manual (`workflow_dispatch`) only
- **File**: `.github/workflows/e2e-test.yml`
- **Inputs** (manual trigger):
  - `cleanup` — Run `azd down` after tests (default: `true`)
  - `environment` — azd environment name (default: auto-generated from run ID)
  - `location` — Azure region (default: `japaneast`)

> **Note**: This workflow is manual-only. The container app pulls its image using a managed identity whose `AcrPull` role assignment is created during `azd provision`, which requires the deployment principal to hold `Microsoft.Authorization/roleAssignments/write` (e.g. **Role Based Access Control Administrator**), not just **Contributor**. The CI principal also cannot create Entra app registrations, so this workflow sets `AUTH_DISABLED=true` to deploy the endpoint unauthenticated (the [opt-out](#deploy-without-authentication-opt-in) above). Pull requests are validated by the credential-free [Build Check](#build-check) workflow instead.

**Smoke test**: After deployment, the "Smoke test deployed MCP server" step calls the live endpoint (MCP `initialize` → `tools/list` → `tools/call`) and runs [`scripts/verify_mcp_response.py`](scripts/verify_mcp_response.py), which parses the response and asserts the returned records are well-formed and exactly match the repo's `data/reference-usecases/` source data.

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
