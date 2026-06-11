extension microsoftGraphV1

// Provisions only the Entra app registration that represents the MCP endpoint as a
// protected API. Authorization is enforced in the MCP server itself (RFC 9728
// Protected Resource Metadata + JWT validation), so no Container Apps built-in (Easy)
// auth config is created here: today Easy Auth returns a bare 401 without the
// resource-metadata pointer the MCP authorization spec requires. If Easy Auth ships
// RFC 9728 support (the App Service WEBSITE_AUTH_PRM_DEFAULT_WITH_SCOPES preview),
// this could be revisited to move token validation back to the platform.

@description('Display name for the endpoint Entra app registration.')
param appDisplayName string

@minLength(1)
@description('Stable suffix used for a deterministic uniqueName and identifier URI.')
param resourceToken string

// The api://{tenantId}/... form is accepted under the default tenant policy without a
// verified domain, and avoids referencing the app's own (server-assigned) appId — which
// Bicep cannot self-reference within the same resource.
var identifierUri = 'api://${tenant().tenantId}/usecase-coach-${resourceToken}'
var scopeName = 'user_impersonation'
var scopeId = guid(resourceToken, 'mcp-user-impersonation')

resource entraApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: 'ca-mcp-${resourceToken}'
  displayName: appDisplayName
  signInAudience: 'AzureADMyOrg'
  identifierUris: [identifierUri]
  api: {
    // v2 tokens carry the app's client ID as the audience, matching the server's validation.
    requestedAccessTokenVersion: 2
    oauth2PermissionScopes: [
      {
        id: scopeId
        value: scopeName
        type: 'User'
        isEnabled: true
        adminConsentDisplayName: 'Access usecase-coach MCP'
        adminConsentDescription: 'Allow the app to call the usecase-coach MCP server as the signed-in user.'
        userConsentDisplayName: 'Access usecase-coach MCP'
        userConsentDescription: 'Allow the app to call the usecase-coach MCP server on your behalf.'
      }
    ]
  }
}

resource entraAppSp 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: entraApp.appId
}

output clientId string = entraApp.appId
output scope string = '${identifierUri}/${scopeName}'
