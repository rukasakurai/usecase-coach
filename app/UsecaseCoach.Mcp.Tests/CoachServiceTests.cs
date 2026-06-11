using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Xunit;
using UsecaseCoach.Mcp;
using UsecaseCoach.Mcp.Coaching;

namespace UsecaseCoach.Mcp.Tests;

public class CoachServiceTests
{
    private static readonly IReadOnlyList<ReferenceUsecase> SampleUsecases =
    [
        new ReferenceUsecase("A problem.", "An approach.", "An impact."),
    ];

    [Fact]
    public async Task CoachAsync_WhenNotConfigured_Throws()
    {
        var service = new CoachService(chatClient: null, () => SampleUsecases);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CoachAsync("I'm good at writing."));
    }

    [Fact]
    public void IsConfigured_ReflectsWhetherAModelIsWired()
    {
        Assert.False(new CoachService(chatClient: null).IsConfigured);
        Assert.True(new CoachService(new FakeChatClient("ok"), () => SampleUsecases).IsConfigured);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CoachAsync_WithBlankConversation_Throws(string conversation)
    {
        var service = new CoachService(new FakeChatClient("ok"), () => SampleUsecases);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CoachAsync(conversation));
    }

    [Fact]
    public async Task CoachAsync_SendsSocraticSystemPromptAndReturnsModelReply()
    {
        var fake = new FakeChatClient("What energizes you most at work?");
        var service = new CoachService(fake, () => SampleUsecases);

        var reply = await service.CoachAsync("I'm good at data; status meetings drain me.");

        Assert.Equal("What energizes you most at work?", reply);
        Assert.Equal(ChatRole.System, fake.LastMessages![0].Role);
        Assert.Contains("Socratic", fake.LastMessages![0].Text);
        Assert.Equal("I'm good at data; status meetings drain me.", fake.LastMessages![1].Text);
    }

    [Fact]
    public async Task CoachAsync_WithOverlongConversation_Throws()
    {
        var service = new CoachService(new FakeChatClient("ok"), () => SampleUsecases);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CoachAsync(new string('x', 8001)));
    }

    [Fact]
    public async Task CoachAsync_CapsModelOutput()
    {
        var fake = new FakeChatClient("ok");
        var service = new CoachService(fake, () => SampleUsecases);

        await service.CoachAsync("I'm good at data.");

        Assert.Equal(400, fake.LastOptions?.MaxOutputTokens);
    }

    private sealed class FakeChatClient(string reply) : IChatClient
    {
        public IList<ChatMessage>? LastMessages { get; private set; }
        public ChatOptions? LastOptions { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToList();
            LastOptions = options;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToList();
            yield return new ChatResponseUpdate(ChatRole.Assistant, reply);
            await Task.CompletedTask;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
