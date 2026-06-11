using System.ComponentModel.DataAnnotations;

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
    [Required]
    public string? TenantId { get; set; }

    /// <summary>
    /// Azure AD app client ID (app registration ID). Required when Enabled is true.
    /// </summary>
    [Required]
    public string? ClientId { get; set; }

    /// <summary>
    /// OAuth 2.0 scope for the protected API. Required when Enabled is true.
    /// </summary>
    [Required]
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
