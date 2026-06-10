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

var resourceToken = uniqueString(resourceGroup().id, environmentName)
var tags = { 'azd-env-name': environmentName }
var placeholderImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

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
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
  dependsOn: [acrPull]
}

// Microsoft Entra ID auth for the endpoint, provisioned in this deployment (app
// registration + service principal + Container Apps auth config) so protecting a
// deployment needs no hand-registered app and no client/tenant IDs copied into config.
// Kept in a conditional module so its Microsoft Graph extension is engaged only when
// auth is enabled — when disableAuth is set the deployment touches no directory objects,
// suiting CI identities that cannot create them. The endpoint is then public.
module authModule 'auth.bicep' = if (!disableAuth) {
  name: 'mcp-auth'
  params: {
    containerAppName: mcp.name
    appDisplayName: 'usecase-coach MCP (${environmentName})'
    resourceToken: resourceToken
  }
}

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.properties.loginServer
output SERVICE_MCP_URI string = 'https://${mcp.properties.configuration.ingress.fqdn}'
output MCP_AUTH_ENABLED bool = !disableAuth
output MCP_ENTRA_CLIENT_ID string = disableAuth ? '' : authModule!.outputs.clientId
