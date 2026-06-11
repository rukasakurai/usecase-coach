targetScope = 'resourceGroup'

@minLength(1)
@description('Name of the azd environment; used for tagging and resource naming.')
param environmentName string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Container image for the MCP service. azd sets this after building/pushing; empty uses a placeholder for the first provision.')
param mcpImageName string = ''

@description('Deploy the endpoint WITHOUT authentication (public). Defaults to false: the endpoint requires Microsoft Entra ID auth and its app registration is provisioned by this deployment. Set to true only where the deploying identity cannot create Entra app registrations (e.g. CI).')
param disableAuth bool = false

@description('Name of the Foundry chat model the coach uses (OpenAI format).')
param foundryModelName string = 'gpt-4o-mini'

@description('Version of the Foundry chat model.')
param foundryModelVersion string = '2024-07-18'

@description('Name of the model deployment the coach calls; also passed to the app as Foundry__DeploymentName.')
param foundryDeploymentName string = 'gpt-4o-mini'

@description('Provisioned throughput (thousands of tokens per minute) for the model deployment.')
param foundryModelCapacity int = 10

var resourceToken = uniqueString(resourceGroup().id, environmentName)
var tags = { 'azd-env-name': environmentName }
var placeholderImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
// Cognitive Services OpenAI User: lets the container's managed identity call the model with Entra tokens (no keys).
var openAiUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${resourceToken}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: 'acr${resourceToken}'
  location: location
  tags: tags
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
  }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${resourceToken}'
  location: location
  tags: tags
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, acrPullRoleId)
  scope: registry
  properties: {
    roleDefinitionId: acrPullRoleId
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Microsoft Foundry account (Cognitive Services, kind AIServices) hosting the chat model
// the Socratic coach calls. Local (key) auth is disabled so the model is reachable only
// with Microsoft Entra tokens — the container uses its managed identity, no keys in source.
resource foundry 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: 'aif-${resourceToken}'
  location: location
  tags: tags
  kind: 'AIServices'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: 'aif-${resourceToken}'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundry
  name: foundryDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: foundryModelCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: foundryModelName
      version: foundryModelVersion
    }
  }
}

resource foundryRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundry.id, identity.id, openAiUserRoleId)
  scope: foundry
  properties: {
    roleDefinitionId: openAiUserRoleId
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource mcp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-mcp-${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': 'mcp' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: containerEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: registry.properties.loginServer
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'mcp'
          image: empty(mcpImageName) ? placeholderImage : mcpImageName
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: concat(mcpAuthEnv, foundryEnv)
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
  dependsOn: [acrPull, foundryRole, chatDeployment]
}

// The MCP server enforces Microsoft Entra ID auth itself (RFC 9728 Protected Resource
// Metadata + JWT validation). The app registration that represents the endpoint as a
// protected API is provisioned in a conditional module so its Microsoft Graph extension
// is engaged only when auth is enabled — when disableAuth is set the deployment touches
// no directory objects (suiting CI identities that cannot create them) and the endpoint
// is public.
module authModule 'auth.bicep' = if (!disableAuth) {
  name: 'mcp-auth'
  params: {
    appDisplayName: 'usecase-coach MCP (${environmentName})'
    resourceToken: resourceToken
  }
}

// Auth settings passed to the container (Mcp:Auth:* configuration). When auth is
// disabled the server runs public; otherwise it validates tokens for this deployment's
// own app registration.
var mcpAuthEnv = disableAuth
  ? [
      { name: 'Mcp__Auth__Enabled', value: 'false' }
    ]
  : [
      { name: 'Mcp__Auth__Enabled', value: 'true' }
      { name: 'Mcp__Auth__TenantId', value: tenant().tenantId }
      { name: 'Mcp__Auth__ClientId', value: authModule!.outputs.clientId }
      { name: 'Mcp__Auth__Scope', value: authModule!.outputs.scope }
    ]

// Foundry settings passed to the container (Foundry:* configuration). The coach reaches
// the model with the container's managed identity, so no key or secret is passed here.
// AZURE_CLIENT_ID tells DefaultAzureCredential which user-assigned identity to use.
var foundryEnv = [
  { name: 'Foundry__Endpoint', value: foundry.properties.endpoint }
  { name: 'Foundry__DeploymentName', value: foundryDeploymentName }
  { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
]

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.properties.loginServer
output SERVICE_MCP_URI string = 'https://${mcp.properties.configuration.ingress.fqdn}'
output MCP_AUTH_ENABLED bool = !disableAuth
output MCP_ENTRA_CLIENT_ID string = disableAuth ? '' : authModule!.outputs.clientId
output MCP_ENTRA_SCOPE string = disableAuth ? '' : authModule!.outputs.scope
output FOUNDRY_ENDPOINT string = foundry.properties.endpoint
output FOUNDRY_DEPLOYMENT_NAME string = foundryDeploymentName
