using System.Collections.Generic;
using System.Linq;
using Xunit;
using UsecaseCoach.Mcp;
using UsecaseCoach.Mcp.Coaching;

namespace UsecaseCoach.Mcp.Tests;

public class CoachPromptTests
{
    private static readonly IReadOnlyList<ReferenceUsecase> SampleUsecases =
    [
        new ReferenceUsecase("Tickets sat in a queue.", "Trained a classifier to route them.", "Response time dropped."),
    ];

    [Fact]
    public void BuildSystemPrompt_EncodesSocraticBehaviour()
    {
        var prompt = CoachPrompt.BuildSystemPrompt(SampleUsecases);

        Assert.Contains("Socratic", prompt);
        Assert.Contains("asking questions", prompt);
        Assert.Contains("strengths and pain points", prompt);
        Assert.Contains("analogies", prompt);
        Assert.Contains("impact", prompt);
        // Reference use cases are included only as analogy material.
        Assert.Contains("Tickets sat in a queue.", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_WithNoUsecases_OmitsReferenceSection()
    {
        var prompt = CoachPrompt.BuildSystemPrompt([]);

        Assert.DoesNotContain("Reference use cases", prompt);
    }

    [Fact]
    public void BuildMessages_PutsSystemPromptFirstAndConversationAsUser()
    {
        var messages = CoachPrompt.BuildMessages("I'm good at data; meetings drain me.", SampleUsecases);

        Assert.Equal(2, messages.Count);
        Assert.Equal(Microsoft.Extensions.AI.ChatRole.System, messages[0].Role);
        Assert.Equal(Microsoft.Extensions.AI.ChatRole.User, messages[1].Role);
        Assert.Equal("I'm good at data; meetings drain me.", messages[1].Text);
    }
}
