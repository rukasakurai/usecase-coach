# usecase-coach

A Socratic discovery coach for finding AI use cases from one's own strengths and pain-points.

## What This Is

usecase-coach challenges copy-others thinking and judges ideas by the impact they would produce, rather than serving up generic examples to imitate. Reference use cases in this repo are inspiration for analogy, not templates to copy.

**Status: very early — concept stage.** No working coach yet.

## Repository Contents

- [`data/reference-usecases/`](data/reference-usecases/) — curated example AI use cases the coach draws on for analogy. Each `*.json` record validates against [`data/reference-usecases/schema.json`](data/reference-usecases/schema.json). See the directory [README](data/reference-usecases/README.md) for the field definitions.
- [`docs/`](docs/) — operational guides (Azure OIDC setup, Copilot coding agent guidance).
- [`CONTRIBUTING.md`](CONTRIBUTING.md) and [`AGENTS.md`](AGENTS.md) — collaboration guidelines for human and AI contributors.

## Included Workflows

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

**Customize for your project**: Edit the "Run tests" step in the workflow to add your E2E test commands (e.g., `pytest tests/e2e/`, `npm test`, or a custom test script).

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
