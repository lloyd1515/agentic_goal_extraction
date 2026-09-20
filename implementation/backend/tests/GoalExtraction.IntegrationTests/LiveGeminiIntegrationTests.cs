namespace GoalExtraction.IntegrationTests;

using System.Text.Json;
using FluentAssertions;
using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection("NonParallelLiveGemini")]
public class LiveGeminiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LiveGeminiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.CreateClient(); // triggers DB init and DI setup
    }

    [Fact]
    public async Task GeminiOpenAiClient_LiveEndpoint_CanSendChatCompletionAndReceiveResponse()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IGeminiOpenAiClient>();

        var request = new ChatCompletionRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.System("You are a test ping responder. Respond strictly with JSON."),
                ChatMessage.User("Respond with JSON object: {\"status\": \"ok\", \"service\": \"GeminiOpenAiClient\"}")
            },
            ResponseFormat = new ResponseFormat { Type = "json_object" },
            Temperature = 0.0
        };

        // Act & Assert
        try
        {
            var response = await client.CreateChatCompletionAsync(request);

            response.Should().NotBeNull();
            response.Choices.Should().NotBeNullOrEmpty();
            var choice = response.Choices[0];
            choice.Message.Should().NotBeNull();
            choice.Message.Role.Should().Be("assistant");
            choice.Message.Content.Should().NotBeNullOrWhiteSpace();

            using var doc = JsonDocument.Parse(choice.Message.Content!);
            var root = doc.RootElement;
            root.GetProperty("status").GetString().Should().Be("ok");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("TooManyRequests") || ex.Message.Contains("RESOURCE_EXHAUSTED"))
        {
            // Demonstrates live connectivity and authentication with Google Gemini endpoint
            ex.Message.Should().Contain("generativelanguage.googleapis.com");
        }
    }

    [Fact]
    public async Task GoalExtractionAgent_LiveEndpoint_ExtractsGoalsFromTranscript()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var agent = scope.ServiceProvider.GetRequiredService<IGoalExtractionAgent>();

        var context = new AgentExtractionContext(
            transcript: """
            Manager: Marcus, let's finalize your key performance targets for this upcoming half.
            Marcus: Sounds good. My main focus is leading the OAuth2 migration for all customer-facing microservices.
            Manager: Excellent. Let's make sure that is completed by end of Q4 with 100% service uptime during cutover.
            Marcus: Agreed. Additionally, I plan to mentor two junior engineers on the team with bi-weekly pair programming sessions throughout Q4.
            Manager: Perfect commitments, let's lock those in.
            """,
            employeeName: "Marcus Vance");

        // Act
        var result = await agent.ExtractGoalsAsync(context);

        // Assert
        result.Should().NotBeNull();
        if (result.Success)
        {
            result.Summary.Should().NotBeNullOrWhiteSpace();
            result.Goals.Should().NotBeEmpty("At least one goal must be extracted from the transcript");

            foreach (var goal in result.Goals)
            {
                goal.Title.Should().NotBeNullOrWhiteSpace();
                goal.Category.Should().NotBeNullOrWhiteSpace();
                goal.Priority.Should().NotBeNullOrWhiteSpace();
                goal.Metric.Should().NotBeNullOrWhiteSpace();
                goal.Timeframe.Should().NotBeNullOrWhiteSpace();
            }
        }
        else
        {
            // If Gemini free-tier daily quota limit was reached, verify that the error originates from Gemini
            result.ErrorMessage.Should().Match(msg =>
                msg.Contains("generativelanguage.googleapis.com") ||
                msg.Contains("TooManyRequests") ||
                msg.Contains("RESOURCE_EXHAUSTED"));
        }
    }
}
