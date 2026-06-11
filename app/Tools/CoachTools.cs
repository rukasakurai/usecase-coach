using System.ComponentModel;
using ModelContextProtocol.Server;
using UsecaseCoach.Mcp.Coaching;

namespace UsecaseCoach.Mcp.Tools;

[McpServerToolType]
public sealed class CoachTools
{
    private readonly CoachService _coach;

    public CoachTools(CoachService coach) => _coach = coach;

    [McpServerTool(Name = "coach")]
    [Description(
        "Socratic discovery coach. Give it the conversation so far — the user's strengths, pain points, " +
        "and any ideas — and it replies with focused questions that draw the use case out of the user, " +
        "using the reference use cases only as analogies and weighing ideas by their likely impact. " +
        "It does not hand over generic examples to copy.")]
    public Task<string> Coach(
        [Description("The conversation so far: the user's strengths, pain points, ideas, and any back-and-forth with the coach.")]
        string conversation,
        CancellationToken cancellationToken)
        => _coach.CoachAsync(conversation, cancellationToken);
}
