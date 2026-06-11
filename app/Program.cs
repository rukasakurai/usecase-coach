using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
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
    .WithTools<ReferenceUsecaseTools>();

// Auth is opt-out: enabled for the Entra-protected Azure deployment, disabled for
// local runs and the public (AUTH_DISABLED) deployment path.
var authEnabled = builder.Configuration.GetValue("Mcp:Auth:Enabled", false);
if (authEnabled)
{
    var tenantId = Required(builder.Configuration, "Mcp:Auth:TenantId");
    var clientId = Required(builder.Configuration, "Mcp:Auth:ClientId");
    var scope = Required(builder.Configuration, "Mcp:Auth:Scope");
    var instance = builder.Configuration["Mcp:Auth:Instance"] ?? "https://login.microsoftonline.com/";
    var authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";

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
            ValidAudiences = [clientId, $"api://{clientId}"],
        };
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            AuthorizationServers = { authority },
            ScopesSupported = [scope],
        };
    });

    builder.Services.AddAuthorization();
}

var app = builder.Build();

app.UseForwardedHeaders();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapGet("/", () => "usecase-coach MCP server. Connect an MCP client to /mcp.");

var mcpEndpoints = app.MapMcp("/mcp");
if (authEnabled)
{
    mcpEndpoints.RequireAuthorization();
}

app.Run();

static string Required(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException(
        $"Configuration '{key}' is required when Mcp:Auth:Enabled is true.");
