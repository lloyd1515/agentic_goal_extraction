namespace GoalExtraction.IntegrationTests;

using FluentAssertions;
using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Application.AI.Tools;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Interfaces;
using GoalExtraction.Infrastructure.AI;
using GoalExtraction.Infrastructure.AI.Options;
using GoalExtraction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class AgentExtractionSecurityIsolationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgentExtractionSecurityIsolationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.CreateClient(); // triggers initialization and seeding
    }

    [Fact]
    public async Task AgentExtractionFlow_PerformsZeroDatabaseInsertsOrUpdates()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GoalExtractionDbContext>();
        var readOnlyRepo = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryRepository>();
        var queryTool = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryTool>();

        // 1. Snapshot database state before extraction
        var initialGoalCount = await dbContext.Goals.AsNoTracking().CountAsync();
        var initialEmployeeCount = await dbContext.Employees.AsNoTracking().CountAsync();
        var initialGoalsSnapshot = await dbContext.Goals.AsNoTracking()
            .Select(g => new { g.Id, g.Title, g.Status, g.CreatedAt })
            .ToListAsync();

        initialGoalCount.Should().BeGreaterThan(0, "Seed data should contain existing goals");
        initialEmployeeCount.Should().BeGreaterThan(0, "Seed data should contain existing employees");

        // 2. Setup mock LLM that triggers tool call and returns extracted goals
        var mockLlm = new Mock<IGeminiOpenAiClient>();
        var elenaId = Guid.Parse("e0a1b2c3-d4e5-0000-0000-000000000001");

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
                                Id = "call_elena",
                                Type = "function",
                                Function = new FunctionCall
                                {
                                    Name = "query_existing_employee_goals",
                                    Arguments = $"{{\"employee_id\": \"{elenaId}\"}}"
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
                          "summary": "Agreed to advance unit test coverage and complete telemetry migration.",
                          "goals": [
                            {
                              "title": "Achieve 85% coverage across core services",
                              "description": "Increase test coverage from current 70% to 85%.",
                              "category": "TECHNICAL",
                              "metric": "85% line coverage in CI",
                              "timeframe": "End of Q4 2026",
                              "priority": "HIGH"
                            }
                          ]
                        }
                        """
                    }
                }
            }
        };

        mockLlm.SetupSequence(c => c.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(turn1Response)
               .ReturnsAsync(turn2Response);

        var agent = new GoalExtractionAgent(
            mockLlm.Object,
            queryTool,
            new JsonSchemaRepairService(),
            Microsoft.Extensions.Options.Options.Create(new GeminiAiOptions()),
            NullLogger<GoalExtractionAgent>.Instance);

        var context = new AgentExtractionContext(
            transcript: "Manager: Great progress on reaching 70% test coverage! Let's aim for 85% by Q4.",
            employeeId: elenaId,
            employeeName: "Elena Rostova");

        // 3. Execute extraction flow
        var extractionResult = await agent.ExtractGoalsAsync(context);

        // 4. Verify extraction succeeded in memory
        extractionResult.Should().NotBeNull();
        extractionResult.Success.Should().BeTrue();
        extractionResult.ToolCallsExecuted.Should().Contain("query_existing_employee_goals");
        extractionResult.Goals.Should().HaveCount(1);

        // 5. Assert database invariant: ZERO inserts, ZERO updates, ZERO deletions
        var postExtractionGoalCount = await dbContext.Goals.AsNoTracking().CountAsync();
        var postExtractionEmployeeCount = await dbContext.Employees.AsNoTracking().CountAsync();
        var postExtractionGoalsSnapshot = await dbContext.Goals.AsNoTracking()
            .Select(g => new { g.Id, g.Title, g.Status, g.CreatedAt })
            .ToListAsync();

        postExtractionGoalCount.Should().Be(initialGoalCount, "AI agent extraction must never perform database inserts");
        postExtractionEmployeeCount.Should().Be(initialEmployeeCount, "AI agent extraction must never mutate employee records");
        postExtractionGoalsSnapshot.Should().BeEquivalentTo(initialGoalsSnapshot, "AI agent extraction must leave all database records strictly unmodified");
    }
}
