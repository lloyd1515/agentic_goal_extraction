namespace GoalExtraction.UnitTests.AI;

using FluentAssertions;
using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Application.AI.Tools;
using GoalExtraction.Infrastructure.AI;
using GoalExtraction.Infrastructure.AI.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class GoalExtractionAgentTests
{
    private readonly Mock<IGeminiOpenAiClient> _mockClient;
    private readonly Mock<IReadOnlyGoalQueryTool> _mockTool;
    private readonly JsonSchemaRepairService _jsonRepairService;
    private readonly IOptions<GeminiAiOptions> _options;
    private readonly GoalExtractionAgent _agent;

    public GoalExtractionAgentTests()
    {
        _mockClient = new Mock<IGeminiOpenAiClient>();
        _mockTool = new Mock<IReadOnlyGoalQueryTool>();
        _jsonRepairService = new JsonSchemaRepairService();
        _options = Microsoft.Extensions.Options.Options.Create(new GeminiAiOptions());

        _mockTool.Setup(t => t.ToolName).Returns("query_existing_employee_goals");
        _mockTool.Setup(t => t.GetToolDeclaration()).Returns(new { name = "query_existing_employee_goals" });

        _agent = new GoalExtractionAgent(
            _mockClient.Object,
            _mockTool.Object,
            _jsonRepairService,
            _options,
            NullLogger<GoalExtractionAgent>.Instance);
    }

    [Fact]
    public async Task ExtractAsync_WhenLlmRequestsToolCall_InvokesToolAndReturnsGoals()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var context = new AgentExtractionContext(
            transcript: "Manager: Let's follow up on your test coverage goal and push it to 80% this quarter.",
            employeeId: employeeId,
            employeeName: "Elena Rostova");

        // Turn 1 response: LLM requests tool call
        var turn1Response = new ChatCompletionResponse
        {
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        ToolCalls = new List<ToolCall>
                        {
                            new()
                            {
                                Id = "call_abc123",
                                Type = "function",
                                Function = new FunctionCall
                                {
                                    Name = "query_existing_employee_goals",
                                    Arguments = $"{{\"employee_id\": \"{employeeId}\"}}"
                                }
                            }
                        }
                    }
                }
            }
        };

        // Turn 2 response: LLM provides final structured JSON
        var finalJson = """
        {
          "summary": "Reviewed Elena's previous 70% coverage goal and agreed to raise the target to 80% by Q4.",
          "goals": [
            {
              "title": "Achieve 80% Unit Test Coverage",
              "description": "Increase overall unit test coverage across core modules to 80%.",
              "category": "TECHNICAL",
              "metric": "80% line coverage in CI",
              "timeframe": "End of Q4 2026",
              "priority": "HIGH"
            }
          ]
        }
        """;

        var turn2Response = new ChatCompletionResponse
        {
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = finalJson
                    }
                }
            }
        };

        _mockClient.SetupSequence(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(turn1Response)
                   .ReturnsAsync(turn2Response);

        _mockTool.Setup(t => t.ExecuteAsync(It.Is<string>(s => s.Contains(employeeId.ToString())), It.IsAny<CancellationToken>()))
                 .ReturnsAsync("{\"count\": 1, \"goals\": [{\"title\": \"Increase coverage to 70%\"}]}");

        // Act
        var result = await _agent.ExtractGoalsAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ToolCallsExecuted.Should().Contain("query_existing_employee_goals");
        result.Goals.Should().HaveCount(1);
        result.Goals[0].Title.Should().Be("Achieve 80% Unit Test Coverage");
        result.Goals[0].Metric.Should().Be("80% line coverage in CI");

        _mockTool.Verify(t => t.ExecuteAsync(It.Is<string>(s => s.Contains(employeeId.ToString())), It.IsAny<CancellationToken>()), Times.Once);
        _mockClient.Verify(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExtractAsync_SmallTalkTranscript_ReturnsEmptyGoalsList()
    {
        // Arrange
        var context = new AgentExtractionContext(
            transcript: "Alice: How was your weekend? Bob: Great, went hiking! Alice: Nice weather lately.");

        var smallTalkResponse = new ChatCompletionResponse
        {
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = """
                        {
                          "summary": "Casual conversation regarding weekend activities and weather; no actionable performance or engineering goals discussed.",
                          "goals": []
                        }
                        """
                    }
                }
            }
        };

        _mockClient.Setup(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(smallTalkResponse);

        // Act
        var result = await _agent.ExtractGoalsAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Goals.Should().BeEmpty();
        result.Summary.Should().Contain("Casual conversation");
        result.ToolCallsExecuted.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_InvalidJsonFirstAttempt_TriggersRepairPromptAndSucceeds()
    {
        // Arrange
        var context = new AgentExtractionContext(transcript: "Manager: Deliver project X by Q3.");

        // First attempt returns malformed JSON
        var brokenResponse = new ChatCompletionResponse
        {
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = "Not a json payload at all!"
                    }
                }
            }
        };

        // Repair attempt returns valid JSON
        var repairedJson = """
        {
          "summary": "Deliver project X.",
          "goals": [
            {
              "title": "Deliver Project X",
              "description": "Complete all deliverables for Project X.",
              "category": "PROJECT",
              "metric": "Deployed to Production",
              "timeframe": "Q3",
              "priority": "HIGH"
            }
          ]
        }
        """;

        var repairedResponse = new ChatCompletionResponse
        {
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = repairedJson
                    }
                }
            }
        };

        _mockClient.SetupSequence(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(brokenResponse)
                   .ReturnsAsync(repairedResponse);

        // Act
        var result = await _agent.ExtractGoalsAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Goals.Should().HaveCount(1);
        result.Goals[0].Title.Should().Be("Deliver Project X");
        _mockClient.Verify(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExtractAsync_WhenClientThrows_ReturnsResultWithFailureAndErrorMessage()
    {
        // Arrange
        var context = new AgentExtractionContext(transcript: "Some discussion");
        _mockClient.Setup(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new HttpRequestException("Connection timeout"));

        // Act
        var result = await _agent.ExtractGoalsAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection timeout");
    }

    [Fact]
    public async Task ExtractAsync_ExceedsMaxTurns_ReturnsFailureResult()
    {
        // Arrange
        var context = new AgentExtractionContext(transcript: "Infinite tool loop transcript");
        var toolCallResponse = new ChatCompletionResponse
        {
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        ToolCalls = new List<ToolCall>
                        {
                            new()
                            {
                                Id = "call_loop",
                                Type = "function",
                                Function = new FunctionCall
                                {
                                    Name = "query_existing_employee_goals",
                                    Arguments = "{}"
                                }
                            }
                        }
                    }
                }
            }
        };

        _mockClient.Setup(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(toolCallResponse);

        _mockTool.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync("{}");

        // Act
        var result = await _agent.ExtractGoalsAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Exceeded maximum tool calling turns");
        _mockClient.Verify(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExtractAsync_UnrecognizedToolRequested_FeedsErrorMessageBackAndProceeds()
    {
        // Arrange
        var context = new AgentExtractionContext(transcript: "Transcript requesting unknown tool");
        var turn1Response = new ChatCompletionResponse
        {
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        ToolCalls = new List<ToolCall>
                        {
                            new()
                            {
                                Id = "call_unknown",
                                Type = "function",
                                Function = new FunctionCall
                                {
                                    Name = "custom_calculator_tool",
                                    Arguments = "{\"expr\": \"1+1\"}"
                                }
                            }
                        }
                    }
                }
            }
        };

        var turn2Response = new ChatCompletionResponse
        {
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = """
                        {
                          "summary": "Processed after unknown tool rejected.",
                          "goals": []
                        }
                        """
                    }
                }
            }
        };

        _mockClient.SetupSequence(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(turn1Response)
                   .ReturnsAsync(turn2Response);

        // Act
        var result = await _agent.ExtractGoalsAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ToolCallsExecuted.Should().Contain("custom_calculator_tool");
        result.Summary.Should().Contain("Processed after unknown tool rejected");
    }

    [Fact]
    public async Task ExtractAsync_LlmReturnsEmptyChoices_ReturnsFailureResult()
    {
        // Arrange
        var context = new AgentExtractionContext(transcript: "Transcript");
        var emptyResponse = new ChatCompletionResponse
        {
            Choices = new List<ChatChoice>()
        };

        _mockClient.Setup(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(emptyResponse);

        // Act
        var result = await _agent.ExtractGoalsAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty choices");
    }
}
