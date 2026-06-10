extension microsoftGraphV1

@description('Name of the Container App to protect with built-in Entra ID auth.')
param containerAppName string

@description('Display name for the endpoint Entra app registration.')
param appDisplayName string

@minLength(1)
@description('Stable suffix used to give the app registration a deterministic uniqueName.')
param resourceToken string

resource mcp 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: containerAppName
}

resource entraApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: 'ca-mcp-${resourceToken}'
  displayName: appDisplayName
  signInAudience: 'AzureADMyOrg'
}

resource entraAppSp 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: entraApp.appId
}

// Return401 (rather than a browser redirect) suits non-interactive MCP clients.
resource auth 'Microsoft.App/containerApps/authConfigs@2024-03-01' = {
  parent: mcp
  name: 'current'
  properties: {
    platform: { enabled: true }
    globalValidation: { unauthenticatedClientAction: 'Return401' }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: entraApp.appId
          openIdIssuer: '${environment().authentication.loginEndpoint}${tenant().tenantId}/v2.0'
        }
      }
    }
  }
  dependsOn: [entraAppSp]
}

output clientId string = entraApp.appId
