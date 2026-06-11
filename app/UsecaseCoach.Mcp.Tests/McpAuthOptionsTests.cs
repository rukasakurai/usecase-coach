using Xunit;
using UsecaseCoach.Mcp;

namespace UsecaseCoach.Mcp.Tests;

public class McpAuthOptionsTests
{
    [Theory]
    [InlineData("https://login.microsoftonline.com/", "abc-tenant-id", "https://login.microsoftonline.com/abc-tenant-id/v2.0")]
    [InlineData("https://login.microsoftonline.com", "abc-tenant-id", "https://login.microsoftonline.com/abc-tenant-id/v2.0")]
    [InlineData("https://custom.auth.example.com/", "xyz", "https://custom.auth.example.com/xyz/v2.0")]
    [InlineData("https://custom.auth.example.com", "xyz", "https://custom.auth.example.com/xyz/v2.0")]
    public void GetAuthority_ConstructsUrlCorrectly_WithAndWithoutTrailingSlash(
        string instance,
        string tenantId,
        string expectedAuthority)
    {
        var options = new McpAuthOptions
        {
            Instance = instance,
            TenantId = tenantId,
        };

        var authority = options.GetAuthority();

        Assert.Equal(expectedAuthority, authority);
    }

    [Fact]
    public void Validate_Disabled_SucceedsEvenWithNoTenantClientOrScope()
    {
        var options = new McpAuthOptions { Enabled = false };

        var result = new ValidateMcpAuthOptions().Validate(null, options);

        Assert.True(result.Succeeded,
            "Validation should succeed when auth is disabled, even with no TenantId/ClientId/Scope.");
    }

    [Fact]
    public void Validate_EnabledWithAllRequiredFields_Succeeds()
    {
        var options = new McpAuthOptions
        {
            Enabled = true,
            TenantId = "my-tenant",
            ClientId = "my-client",
            Scope = "api://my-client/.default",
        };

        var result = new ValidateMcpAuthOptions().Validate(null, options);

        Assert.True(result.Succeeded, $"Validation should succeed; failures: {result.FailureMessage}");
    }

    [Theory]
    [InlineData(null, "my-client", "api://my-client/.default", nameof(McpAuthOptions.TenantId))]
    [InlineData("my-tenant", null, "api://my-client/.default", nameof(McpAuthOptions.ClientId))]
    [InlineData("my-tenant", "my-client", null, nameof(McpAuthOptions.Scope))]
    public void Validate_EnabledWithMissingField_Fails(
        string? tenantId,
        string? clientId,
        string? scope,
        string expectedMissingField)
    {
        var options = new McpAuthOptions
        {
            Enabled = true,
            TenantId = tenantId,
            ClientId = clientId,
            Scope = scope,
        };

        var result = new ValidateMcpAuthOptions().Validate(null, options);

        Assert.True(result.Failed, "Validation should fail when auth is enabled but a value is missing.");
        Assert.Contains(expectedMissingField, result.FailureMessage);
    }
}
