using System.Text;
using Microsoft.Extensions.AI;

namespace UsecaseCoach.Mcp.Coaching;

/// <summary>
/// Builds the chat messages that turn a Foundry model into a Socratic discovery coach.
/// Pure functions over the reference use cases and the conversation so far, so the
/// coaching behaviour can be unit-tested without calling a live model.
/// </summary>
public static class CoachPrompt
{
    public static string BuildSystemPrompt(IReadOnlyList<ReferenceUsecase> referenceUsecases)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a Socratic discovery coach. Your job is to help the user find an AI use case that fits them — not to hand them one.");
        sb.AppendLine();
        sb.AppendLine("How you work:");
        sb.AppendLine("- Guide primarily by asking questions. Do not serve up generic examples to imitate or ready-made answers to copy.");
        sb.AppendLine("- Draw the idea out of the user's own strengths and pain points; ask about these before suggesting any direction.");
        sb.AppendLine("- Use the reference use cases below only as analogies to provoke the user's thinking, never as templates to copy.");
        sb.AppendLine("- Weigh ideas by the impact they would produce, not by how common or impressive they sound.");
        sb.AppendLine("- Keep replies short: usually one or two focused questions.");

        if (referenceUsecases.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Reference use cases (for analogy only):");
            foreach (var usecase in referenceUsecases)
            {
                sb.AppendLine($"- Problem: {usecase.Problem} Approach: {usecase.Approach} Impact: {usecase.Impact}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    public static IList<ChatMessage> BuildMessages(
        string conversation,
        IReadOnlyList<ReferenceUsecase> referenceUsecases) =>
        [
            new ChatMessage(ChatRole.System, BuildSystemPrompt(referenceUsecases)),
            new ChatMessage(ChatRole.User, conversation),
        ];
}
