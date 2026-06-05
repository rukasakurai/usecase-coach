using System.ComponentModel;
using ModelContextProtocol.Server;

namespace UsecaseCoach.Mcp.Tools;

[McpServerToolType]
public sealed class ReferenceUsecaseTools
{
    [McpServerTool(Name = "get_reference_usecases")]
    [Description("Returns curated reference AI use cases (problem, approach, impact) to draw on for analogy. These are inspiration, not templates to copy.")]
    public static IReadOnlyList<ReferenceUsecase> GetReferenceUsecases()
        => ReferenceUsecaseStore.LoadAll();
}
