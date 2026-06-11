using System.ComponentModel.DataAnnotations;

namespace UsecaseCoach.Mcp;

/// <summary>
/// Configuration for MCP Entra authentication.
/// Bound from Mcp:Auth configuration section.
/// </summary>
public class McpAuthOptions : IValidatableObject
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

    /// <summary>
    /// DataAnnotations validation rule: these values are required only when
    /// authentication is enabled.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(TenantId))
        {
            yield return new ValidationResult(
                "TenantId is required when Mcp:Auth:Enabled is true.",
                [nameof(TenantId)]);
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            yield return new ValidationResult(
                "ClientId is required when Mcp:Auth:Enabled is true.",
                [nameof(ClientId)]);
        }

        if (string.IsNullOrWhiteSpace(Scope))
        {
            yield return new ValidationResult(
                "Scope is required when Mcp:Auth:Enabled is true.",
                [nameof(Scope)]);
        }
    }
}
