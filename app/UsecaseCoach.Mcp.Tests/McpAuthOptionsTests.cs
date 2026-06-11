using System.ComponentModel.DataAnnotations;
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
        // Arrange
        var options = new McpAuthOptions
        {
            Instance = instance,
            TenantId = tenantId,
        };

        // Act
        var authority = options.GetAuthority();

        // Assert
        Assert.Equal(expectedAuthority, authority);
    }

    [Fact]
    public void Validate_MissingTenantId_FailsValidation()
    {
        // Arrange
        var options = new McpAuthOptions
        {
            ClientId = "my-client",
            Scope = "api://my-client/.default",
            // TenantId is null/missing
        };

        // Act
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(results);
        Assert.True(results.Any(r => r.MemberNames.Contains(nameof(McpAuthOptions.TenantId))),
            "Validation should fail on missing TenantId");
    }

    [Fact]
    public void Validate_MissingClientId_FailsValidation()
    {
        // Arrange
        var options = new McpAuthOptions
        {
            TenantId = "my-tenant",
            Scope = "api://my-client/.default",
            // ClientId is null/missing
        };

        // Act
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.True(results.Any(r => r.MemberNames.Contains(nameof(McpAuthOptions.ClientId))),
            "Validation should fail on missing ClientId");
    }

    [Fact]
    public void Validate_MissingScope_FailsValidation()
    {
        // Arrange
        var options = new McpAuthOptions
        {
            TenantId = "my-tenant",
            ClientId = "my-client",
            // Scope is null/missing
        };

        // Act
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.True(results.Any(r => r.MemberNames.Contains(nameof(McpAuthOptions.Scope))),
            "Validation should fail on missing Scope");
    }

    [Fact]
    public void Validate_AllRequiredFieldsPresent_Succeeds()
    {
        // Arrange
        var options = new McpAuthOptions
        {
            TenantId = "my-tenant",
            ClientId = "my-client",
            Scope = "api://my-client/.default",
        };

        // Act
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        Assert.True(isValid, $"Validation should succeed; errors: {string.Join(", ", results.Select(r => r.ErrorMessage))}");
    }
}
