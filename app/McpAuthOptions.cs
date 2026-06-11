using Microsoft.Extensions.Options;

namespace UsecaseCoach.Mcp;

/// <summary>
/// Configuration for MCP Entra authentication.
/// Bound from Mcp:Auth configuration section.
/// </summary>
public class McpAuthOptions
{
    /// <summary>
    /// Whether to enable authentication. Defaults to false (public endpoint).
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Azure AD tenant ID. Required when Enabled is true.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Azure AD app client ID (app registration ID). Required when Enabled is true.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// OAuth 2.0 scope for the protected API. Required when Enabled is true.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Azure AD authentication endpoint base URL.
    /// Defaults to Microsoft's public endpoint.
    /// </summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// Constructs the OAuth 2.0 authority URL (e.g., for JWT validation).
    /// Handles trailing slashes in Instance correctly.
    /// </summary>
    public string GetAuthority()
    {
        var baseUrl = Instance.TrimEnd('/');
        return $"{baseUrl}/{TenantId}/v2.0";
    }
}

/// <summary>
/// Validates that the Entra values needed for token validation are present,
/// but only when authentication is enabled. When disabled, the endpoint is
/// public and no TenantId/ClientId/Scope are required.
/// </summary>
public sealed class ValidateMcpAuthOptions : IValidateOptions<McpAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, McpAuthOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(options.TenantId)) missing.Add(nameof(McpAuthOptions.TenantId));
        if (string.IsNullOrWhiteSpace(options.ClientId)) missing.Add(nameof(McpAuthOptions.ClientId));
        if (string.IsNullOrWhiteSpace(options.Scope)) missing.Add(nameof(McpAuthOptions.Scope));

        return missing.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"Mcp:Auth:Enabled is true but required values are missing: {string.Join(", ", missing)}.");
    }
}
