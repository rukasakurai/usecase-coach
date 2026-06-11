using Microsoft.Extensions.AI;

namespace UsecaseCoach.Mcp.Coaching;

/// <summary>
/// Runs the Socratic coaching turn against a Microsoft Foundry chat model.
/// The model client is optional so the host still starts when Foundry is not
/// configured (e.g. local runs without a deployment); in that case the coach
/// reports that it needs configuration instead of failing at startup.
/// </summary>
public sealed class CoachService
{
    // Bound per-request work so a single caller cannot drive unbounded model
    // token cost/latency (the coach only needs the conversation so far, and its
    // replies are one or two short questions).
    private const int MaxConversationChars = 8000;
    private const int MaxOutputTokens = 400;

    private readonly IChatClient? _chatClient;
    private readonly Func<IReadOnlyList<ReferenceUsecase>> _referenceUsecasesProvider;

    public CoachService(
        IChatClient? chatClient = null,
        Func<IReadOnlyList<ReferenceUsecase>>? referenceUsecasesProvider = null)
    {
        _chatClient = chatClient;
        _referenceUsecasesProvider = referenceUsecasesProvider ?? ReferenceUsecaseStore.LoadAll;
    }

    /// <summary>Whether a Foundry chat model is configured and the coach can respond.</summary>
    public bool IsConfigured => _chatClient is not null;

    public async Task<string> CoachAsync(string conversation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversation))
        {
            throw new ArgumentException(
                "Share the conversation so far — the user's strengths, pain points, and any ideas — so the coach has something to work with.",
                nameof(conversation));
        }

        if (conversation.Length > MaxConversationChars)
        {
            throw new ArgumentException(
                $"The conversation is too long ({conversation.Length} characters); keep it under {MaxConversationChars}.",
                nameof(conversation));
        }

        if (_chatClient is null)
        {
            throw new InvalidOperationException(
                "The coach requires a Microsoft Foundry chat model. Set Foundry:Endpoint and Foundry:DeploymentName " +
                "(env vars Foundry__Endpoint and Foundry__DeploymentName) and grant the host identity access to the deployment.");
        }

        var messages = CoachPrompt.BuildMessages(conversation, _referenceUsecasesProvider());
        var options = new ChatOptions { MaxOutputTokens = MaxOutputTokens };
        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        return response.Text;
    }
}
