using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace UsecaseCoach.Mcp.Tests;

public class StartupTests
{
    [Fact]
    public async Task Host_StartsAndServesRoot_WhenAuthDisabled()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("Mcp:Auth:Enabled", "false"));

        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Host_FailsToStart_WhenAuthEnabledButMisconfigured()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("Mcp:Auth:Enabled", "true"));

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }
}
