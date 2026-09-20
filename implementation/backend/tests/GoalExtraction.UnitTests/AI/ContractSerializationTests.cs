namespace GoalExtraction.UnitTests.AI;

using System.Text.Json;
using FluentAssertions;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Application.DTOs;
using Xunit;

public class ContractSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ExtractedGoalsResult_SerializationRoundTrip_MaintainsAllFields()
    {
        var original = new ExtractedGoalsResult
        {
            Success = true,
            Summary = "Quarterly review summary",
            ErrorMessage = null,
            ToolCallsExecuted = new List<string> { "query_existing_employee_goals" },
            Goals = new List<ExtractedGoalItem>
            {
                new()
                {
                    Title = "Increase test coverage to 80%",
                    Description = "Write unit and integration tests",
                    Category = "TECHNICAL",
                    Metric = "80% coverage in CI",
                    Timeframe = "End of Q4",
                    Priority = "HIGH"
                }
            }
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ExtractedGoalsResult>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Success.Should().Be(original.Success);
        deserialized.Summary.Should().Be(original.Summary);
        deserialized.Goals.Should().HaveCount(1);
        deserialized.Goals[0].Title.Should().Be("Increase test coverage to 80%");
        deserialized.Goals[0].Category.Should().Be("TECHNICAL");
        deserialized.ToolCallsExecuted.Should().Contain("query_existing_employee_goals");
    }

    [Fact]
    public void ChatCompletionRequest_SerializationMatchesOpenAiFormat()
    {
        var request = new ChatCompletionRequest
        {
            Model = "gemini-2.5-flash",
            Messages = new List<ChatMessage>
            {
                ChatMessage.System("System prompt"),
                ChatMessage.User("User prompt")
            },
            Temperature = 0.1,
            ResponseFormat = new ResponseFormat { Type = "json_object" }
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("model").GetString().Should().Be("gemini-2.5-flash");
        root.GetProperty("temperature").GetDouble().Should().Be(0.1);
        root.GetProperty("messages").GetArrayLength().Should().Be(2);
        root.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_object");
    }

    [Fact]
    public void ChatCompletionResponse_DeserializationFromOpenAiJson_ParsesCorrectly()
    {
        var json = """
        {
          "id": "chatcmpl-test-123",
          "choices": [
            {
              "index": 0,
              "finish_reason": "stop",
              "message": {
                "role": "assistant",
                "content": "{\"summary\": \"Great job\", \"goals\": []}"
              }
            }
          ],
          "usage": {
            "prompt_tokens": 50,
            "completion_tokens": 20,
            "total_tokens": 70
          }
        }
        """;

        var response = JsonSerializer.Deserialize<ChatCompletionResponse>(json, JsonOptions);

        response.Should().NotBeNull();
        response!.Id.Should().Be("chatcmpl-test-123");
        response.Choices.Should().HaveCount(1);
        response.Choices[0].FinishReason.Should().Be("stop");
        response.Choices[0].Message.Role.Should().Be("assistant");
        response.Choices[0].Message.Content.Should().Contain("Great job");
        response.Usage.Should().NotBeNull();
        response.Usage!.TotalTokens.Should().Be(70);
        response.FirstMessage.Should().NotBeNull();
    }

    [Fact]
    public void ToolCallResponse_Deserialization_ParsesToolCallsCorrectly()
    {
        var json = """
        {
          "id": "chatcmpl-tool-123",
          "choices": [
            {
              "index": 0,
              "finish_reason": "tool_calls",
              "message": {
                "role": "assistant",
                "tool_calls": [
                  {
                    "id": "call_abc",
                    "type": "function",
                    "function": {
                      "name": "query_existing_employee_goals",
                      "arguments": "{\"employee_id\": \"e0a1b2c3-d4e5-0000-0000-000000000001\"}"
                    }
                  }
                ]
              }
            }
          ]
        }
        """;

        var response = JsonSerializer.Deserialize<ChatCompletionResponse>(json, JsonOptions);

        response.Should().NotBeNull();
        var toolCalls = response!.Choices[0].Message.ToolCalls;
        toolCalls.Should().NotBeNull();
        toolCalls.Should().HaveCount(1);
        toolCalls![0].Id.Should().Be("call_abc");
        toolCalls[0].Function.Name.Should().Be("query_existing_employee_goals");
        toolCalls[0].Function.Arguments.Should().Contain("e0a1b2c3-d4e5-0000-0000-000000000001");
    }

    [Fact]
    public void ExtractGoalsRequestDto_NestedContext_PopulatesEmployeeIdAndDepartment()
    {
        var json = """
        {
          "transcript": "Elena discussed Q4 goals.",
          "context": {
            "employeeId": "e0a1b2c3-d4e5-0000-0000-000000000001",
            "department": "Software Engineering"
          }
        }
        """;

        var dto = JsonSerializer.Deserialize<ExtractGoalsRequestDto>(json, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Transcript.Should().Be("Elena discussed Q4 goals.");
        dto.Context.Should().NotBeNull();
        dto.Context!.EmployeeId.Should().Be(Guid.Parse("e0a1b2c3-d4e5-0000-0000-000000000001"));
        dto.Context.Department.Should().Be("Software Engineering");
        dto.EmployeeId.Should().Be(Guid.Parse("e0a1b2c3-d4e5-0000-0000-000000000001"));
        dto.Department.Should().Be("Software Engineering");
    }

    [Fact]
    public void ExtractGoalsRequestDto_FlatProperties_PopulatesEmployeeIdAndDepartment()
    {
        var json = """
        {
          "transcript": "Elena discussed Q4 goals.",
          "employeeId": "e0a1b2c3-d4e5-0000-0000-000000000001",
          "department": "Software Engineering"
        }
        """;

        var dto = JsonSerializer.Deserialize<ExtractGoalsRequestDto>(json, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Transcript.Should().Be("Elena discussed Q4 goals.");
        dto.EmployeeId.Should().Be(Guid.Parse("e0a1b2c3-d4e5-0000-0000-000000000001"));
        dto.Department.Should().Be("Software Engineering");
    }

    [Fact]
    public void SaveGoalsBatchResultDto_Serialization_IncludesGoalIdsAndPersistedCount()
    {
        var goalId = Guid.NewGuid();
        var result = new SaveGoalsBatchResultDto
        {
            CorrelationId = "test-corr-1",
            Success = true,
            SavedCount = 1,
            SavedGoalIds = new List<Guid> { goalId }
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify AC 6.4 contract compliance
        root.GetProperty("goalIds").GetArrayLength().Should().Be(1);
        root.GetProperty("goalIds")[0].GetString().Should().Be(goalId.ToString());
        root.GetProperty("persistedCount").GetInt32().Should().Be(1);

        // Verify preserved properties
        root.GetProperty("savedGoalIds").GetArrayLength().Should().Be(1);
        root.GetProperty("savedCount").GetInt32().Should().Be(1);
    }
}
