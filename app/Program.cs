using UsecaseCoach.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<ReferenceUsecaseTools>();

var app = builder.Build();

app.MapGet("/", () => "usecase-coach MCP server. Connect an MCP client to /mcp.");
app.MapMcp("/mcp");

app.Run();
