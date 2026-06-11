using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using UsecaseCoach.Mcp;
using UsecaseCoach.Mcp.Coaching;
using UsecaseCoach.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

// Container Apps terminates TLS at the ingress and forwards over http; honor the
// forwarded scheme/host so the OAuth resource metadata advertises https URLs.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<ReferenceUsecaseTools>()
    .WithTools<CoachTools>();

// Wire the Socratic coach to a Microsoft Foundry chat model when configured. The
// endpoint and deployment name come from configuration (env vars Foundry__Endpoint
// and Foundry__DeploymentName) so no environment-specific values live in source, and
// the model is reached with the host's managed identity (DefaultAzureCredential) — no
// keys are handled. When unconfigured, no IChatClient is registered and the coach tool
// reports that it needs configuration instead of blocking startup.
var foundryEndpoint = builder.Configuration["Foundry:Endpoint"];
var foundryDeployment = builder.Configuration["Foundry:DeploymentName"];
if (!string.IsNullOrWhiteSpace(foundryEndpoint) && !string.IsNullOrWhiteSpace(foundryDeployment))
{
    builder.Services.AddSingleton<IChatClient>(_ =>
        new AzureOpenAIClient(new Uri(foundryEndpoint), new DefaultAzureCredential())
            .GetChatClient(foundryDeployment)
            .AsIChatClient());
}

builder.Services.AddSingleton(sp => new CoachService(sp.GetService<IChatClient>()));

// Bind and validate MCP auth configuration (opt-out: auth enabled by default).
// ValidateOnStart() ensures config errors fail at startup, not at first use.
// Validation is conditional: TenantId/ClientId/Scope are only required when
// Mcp:Auth:Enabled is true (via DataAnnotations + IValidatableObject in
// McpAuthOptions), so the public endpoint starts without them.
builder.Services
    .AddOptions<McpAuthOptions>()
    .BindConfiguration("Mcp:Auth")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Configure authentication if enabled in Mcp:Auth:Enabled
var mcpAuthOptions = builder.Configuration.GetSection("Mcp:Auth").Get<McpAuthOptions>() ?? new();
if (mcpAuthOptions.Enabled)
{
    var authority = mcpAuthOptions.GetAuthority();

    // Validate Entra access tokens in-process and advertise RFC 9728 Protected
    // Resource Metadata so MCP clients (VS Code, Copilot CLI) can run their built-in
    // OAuth 2.1 sign-in. Container Apps' built-in (Easy) auth is intentionally not
    // used here: today it returns a bare 401 without the resource-metadata pointer
    // the MCP authorization spec requires. If Easy Auth ships RFC 9728 support (the
    // App Service WEBSITE_AUTH_PRM_DEFAULT_WITH_SCOPES preview), this can be revisited.
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // v2 access tokens carry the resource app's client ID as the audience.
            // Validation in McpAuthOptions.Validate ensures these are non-null when Enabled is true.
            ValidAudiences = [mcpAuthOptions.ClientId!, $"api://{mcpAuthOptions.ClientId}"],
        };
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            AuthorizationServers = { authority },
            ScopesSupported = [mcpAuthOptions.Scope!],
        };
    });

    builder.Services.AddAuthorization();
}

var app = builder.Build();

app.UseForwardedHeaders();

if (mcpAuthOptions.Enabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapGet("/", () => "usecase-coach MCP server. Connect an MCP client to /mcp.");

var mcpEndpoints = app.MapMcp("/mcp");
if (mcpAuthOptions.Enabled)
{
    mcpEndpoints.RequireAuthorization();
}

app.Run();

// Exposed so integration tests can boot the host with WebApplicationFactory.
public partial class Program { }
