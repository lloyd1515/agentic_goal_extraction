namespace GoalExtraction.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using GoalExtraction.Api.Middleware;
using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Application.DTOs;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using GoalExtraction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

public class EndToEndFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public EndToEndFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.CreateClient(); // Initialize database & seed data
    }

    private HttpClient CreateClientWithMockedLlm(Guid employeeId)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove concrete IGeminiOpenAiClient registration to replace with mock
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IGeminiOpenAiClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var mockLlm = new Mock<IGeminiOpenAiClient>();

                // Turn 1: LLM requests tool execution to inspect existing employee goals
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
                                        Id = "call_query_goals_001",
                                        Type = "function",
                                        Function = new FunctionCall
                                        {
                                            Name = "query_existing_employee_goals",
                                            Arguments = $"{{\"employee_id\": \"{employeeId}\", \"status\": \"ACTIVE\", \"limit\": 10}}"
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                // Turn 2: LLM receives tool output and returns structured goal proposals
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
                                  "summary": "Extracted 2 technical deliverables for Elena Rostova from 1:1 engineering sync.",
                                  "goals": [
                                    {
                                      "title": "Migrate internal auth services to OAuth 2.1",
                                      "description": "Upgrade identity provider and microservice gateways to OAuth 2.1 specifications.",
                                      "category": "TECHNICAL",
                                      "metric": "Zero downtime cutover and 100% token validation pass rate",
                                      "timeframe": "Q3 2026",
                                      "priority": "HIGH"
                                    },
                                    {
                                      "title": "Implement OpenTelemetry distributed tracing",
                                      "description": "Instrument all transaction processing microservices with OpenTelemetry distributed trace spans.",
                                      "category": "TECHNICAL",
                                      "metric": "99.9% span coverage across all production APIs",
                                      "timeframe": "Q4 2026",
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

                services.AddScoped(_ => mockLlm.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task FullEndToEndLifecycle_Extraction_Review_Persistence_AndRollback_Succeeds()
    {
        // -----------------------------------------------------------------------------------------
        // STEP 1: Query /api/employees to obtain Elena Rostova's ID
        // -----------------------------------------------------------------------------------------
        var defaultClient = _factory.CreateClient();
        var empResponse = await defaultClient.GetAsync("/api/employees");
        empResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        empResponse.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var employees = await empResponse.Content.ReadFromJsonAsync<List<EmployeeDto>>(JsonOptions);
        employees.Should().NotBeNull();
        employees!.Should().NotBeEmpty();

        var elena = employees!.FirstOrDefault(e => e.FullName == "Elena Rostova");
        elena.Should().NotBeNull("Elena Rostova must be present in the seeded employee directory");
        var elenaId = elena!.Id;
        elenaId.Should().Be(DatabaseInitializer.ElenaRostovaId);

        // Snapshot database state prior to AI extraction
        using var preExtractionScope = _factory.Services.CreateScope();
        var preExtractionDb = preExtractionScope.ServiceProvider.GetRequiredService<GoalExtractionDbContext>();
        var initialDbGoalCount = await preExtractionDb.Goals.AsNoTracking().CountAsync();
        var initialElenaGoalCount = await preExtractionDb.Goals.AsNoTracking()
            .CountAsync(g => g.EmployeeId == elenaId);

        // -----------------------------------------------------------------------------------------
        // STEP 2: Post realistic discussion transcript to POST /api/goals/extract
        // -----------------------------------------------------------------------------------------
        var client = CreateClientWithMockedLlm(elenaId);

        var transcript = """
            Manager (Marcus): Hi Elena, let's establish your key technical commitments for this upcoming half.
            Elena: Great! My number one priority is migrating our internal auth services to OAuth 2.1 RFC specifications.
            Manager: Excellent. Let's aim to have that completed by Q3 2026 with zero downtime during cutover.
            Elena: Agreed. My second major initiative is implementing OpenTelemetry distributed tracing across all our transaction microservices by Q4 2026.
            Manager: Perfect. That gives us 99.9% visibility into our production transaction pipelines. Let's lock those in.
            """;

        var extractRequest = new ExtractGoalsRequestDto
        {
            Transcript = transcript,
            EmployeeId = elenaId,
            Department = elena.Department
        };

        var extractResponse = await client.PostAsJsonAsync("/api/goals/extract", extractRequest);

        // Verify HTTP 200 OK and response schema compliance
        extractResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        extractResponse.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var extractionResult = await extractResponse.Content.ReadFromJsonAsync<GoalExtractionResultDto>(JsonOptions);
        extractionResult.Should().NotBeNull();
        extractionResult!.TranscriptHash.Should().NotBeNullOrWhiteSpace();
        extractionResult.Summary.Should().NotBeNullOrWhiteSpace();
        extractionResult.Goals.Should().HaveCount(2);
        extractionResult.ToolCallsExecuted.Should().Contain("query_existing_employee_goals");

        foreach (var goal in extractionResult.Goals)
        {
            goal.TempId.Should().NotBeEmpty();
            goal.Title.Should().NotBeNullOrWhiteSpace();
            goal.Description.Should().NotBeNullOrWhiteSpace();
            goal.Category.Should().NotBeNullOrWhiteSpace();
            goal.Metric.Should().NotBeNullOrWhiteSpace();
            goal.Timeframe.Should().NotBeNullOrWhiteSpace();
            goal.Priority.Should().NotBeNullOrWhiteSpace();
            goal.Provenance.Should().Be("AI_ORIGINAL");
        }

        // -----------------------------------------------------------------------------------------
        // STEP 3: Assert that the extraction step performed ZERO database inserts into the Goals table
        // -----------------------------------------------------------------------------------------
        using var postExtractionScope = _factory.Services.CreateScope();
        var postExtractionDb = postExtractionScope.ServiceProvider.GetRequiredService<GoalExtractionDbContext>();
        var postExtractionDbGoalCount = await postExtractionDb.Goals.AsNoTracking().CountAsync();
        var postExtractionElenaGoalCount = await postExtractionDb.Goals.AsNoTracking()
            .CountAsync(g => g.EmployeeId == elenaId);

        postExtractionDbGoalCount.Should().Be(initialDbGoalCount,
            "AI Agent extraction must be strictly read-only and execute zero database inserts");
        postExtractionElenaGoalCount.Should().Be(initialElenaGoalCount,
            "Employee goals count must remain identical before and after AI extraction");

        // -----------------------------------------------------------------------------------------
        // STEP 4: Simulate human modification of goal 1 (AI_MODIFIED) and manual goal addition (MANUAL)
        // -----------------------------------------------------------------------------------------
        var extractedGoal1 = extractionResult.Goals[0];
        var extractedGoal2 = extractionResult.Goals[1];

        // Modified goal 1
        var modifiedGoal1 = new SaveGoalItemDto
        {
            Title = extractedGoal1.Title + " with HSM hardware keys",
            Description = extractedGoal1.Description + " Includes hardware security module key storage.",
            Category = extractedGoal1.Category,
            Metric = "Zero auth downtime and HSM FIPS-140 compliance",
            Timeframe = extractedGoal1.Timeframe,
            Priority = extractedGoal1.Priority,
            Provenance = "AI_MODIFIED"
        };

        // Unmodified goal 2
        var originalGoal2 = new SaveGoalItemDto
        {
            Title = extractedGoal2.Title,
            Description = extractedGoal2.Description,
            Category = extractedGoal2.Category,
            Metric = extractedGoal2.Metric,
            Timeframe = extractedGoal2.Timeframe,
            Priority = extractedGoal2.Priority,
            Provenance = "AI_ORIGINAL"
        };

        // Manual goal addition
        var manualGoal3 = new SaveGoalItemDto
        {
            Title = "Lead cross-team architecture reviews for event streaming",
            Description = "Host bi-weekly engineering review sessions for asynchronous event messaging standards.",
            Category = "DEVELOPMENT",
            Metric = "100% RFC sign-off by tech leads",
            Timeframe = "H2 2026",
            Priority = "MEDIUM",
            Provenance = "MANUAL"
        };

        var batchToSave = new List<SaveGoalItemDto> { modifiedGoal1, originalGoal2, manualGoal3 };

        // -----------------------------------------------------------------------------------------
        // STEP 5: Post the finalized batch to POST /api/goals/batch
        // -----------------------------------------------------------------------------------------
        var saveRequest = new SaveGoalsBatchRequestDto
        {
            EmployeeId = elenaId,
            ReviewerId = DatabaseInitializer.MarcusVanceId,
            SourceTranscriptHash = extractionResult.TranscriptHash,
            Goals = batchToSave
        };

        var saveResponse = await client.PostAsJsonAsync("/api/goals/batch", saveRequest);

        // Verify HTTP 201 Created and response containing saved IDs
        saveResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        saveResponse.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var saveResult = await saveResponse.Content.ReadFromJsonAsync<SaveGoalsBatchResultDto>(JsonOptions);
        saveResult.Should().NotBeNull();
        saveResult!.Success.Should().BeTrue();
        saveResult.SavedCount.Should().Be(3);
        saveResult.SavedGoalIds.Should().HaveCount(3);
        saveResult.SavedGoalIds.Should().OnlyHaveUniqueItems();

        // -----------------------------------------------------------------------------------------
        // STEP 6: Query database directly and via /api/employees/{id}/goals
        // -----------------------------------------------------------------------------------------
        // A) Verify via API endpoint
        var elenaGoalsResponse = await client.GetAsync($"/api/employees/{elenaId}/goals?limit=50");
        elenaGoalsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activeElenaGoals = await elenaGoalsResponse.Content.ReadFromJsonAsync<List<Goal>>(JsonOptions);
        activeElenaGoals.Should().NotBeNull();
        var activeElenaGoalIds = activeElenaGoals!.Select(g => g.Id).ToList();

        foreach (var savedId in saveResult.SavedGoalIds)
        {
            activeElenaGoalIds.Should().Contain(savedId);
        }

        // B) Verify direct database records
        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<GoalExtractionDbContext>();
        var persistedGoals = await verificationDb.Goals.AsNoTracking()
            .Where(g => saveResult.SavedGoalIds.Contains(g.Id))
            .ToListAsync();

        persistedGoals.Should().HaveCount(3);
        persistedGoals.Should().OnlyContain(g => g.Status == GoalStatus.ACTIVE);
        persistedGoals.Should().OnlyContain(g => g.EmployeeId == elenaId);
        persistedGoals.Should().OnlyContain(g => g.ReviewerId == DatabaseInitializer.MarcusVanceId);
        persistedGoals.Should().OnlyContain(g => g.SourceTranscriptHash == extractionResult.TranscriptHash);

        // Verify provenances
        var savedModified = persistedGoals.FirstOrDefault(g => g.Title.Contains("HSM hardware keys"));
        savedModified.Should().NotBeNull();
        savedModified!.Provenance.Should().Be(GoalProvenance.AI_MODIFIED);

        var savedOriginal = persistedGoals.FirstOrDefault(g => g.Title == extractedGoal2.Title);
        savedOriginal.Should().NotBeNull();
        savedOriginal!.Provenance.Should().Be(GoalProvenance.AI_ORIGINAL);

        var savedManual = persistedGoals.FirstOrDefault(g => g.Title.Contains("cross-team architecture reviews"));
        savedManual.Should().NotBeNull();
        savedManual!.Provenance.Should().Be(GoalProvenance.MANUAL);

        // -----------------------------------------------------------------------------------------
        // STEP 7: Post duplicate batch with invalid goal to verify atomic transaction rollback
        // -----------------------------------------------------------------------------------------
        var countBeforeRollback = await verificationDb.Goals.AsNoTracking().CountAsync();

        // 7a. Post batch through API with invalid goal (Title too short: < 5 chars)
        var invalidApiBatch = new SaveGoalsBatchRequestDto
        {
            EmployeeId = elenaId,
            ReviewerId = DatabaseInitializer.MarcusVanceId,
            SourceTranscriptHash = extractionResult.TranscriptHash,
            Goals = new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Valid Goal In Rejected Batch",
                    Description = "Valid description meeting minimum length constraints.",
                    Category = "TECHNICAL",
                    Priority = "HIGH",
                    Provenance = "MANUAL"
                },
                new()
                {
                    Title = "Bad", // Invalid length (< 5 chars)
                    Description = "Valid description for bad goal.",
                    Category = "TECHNICAL",
                    Priority = "LOW",
                    Provenance = "MANUAL"
                }
            }
        };

        var invalidApiResponse = await client.PostAsJsonAsync("/api/goals/batch", invalidApiBatch);
        invalidApiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var countAfterApiValidation = await verificationDb.Goals.AsNoTracking().CountAsync();
        countAfterApiValidation.Should().Be(countBeforeRollback,
            "Validation failure must block execution and leave database untouched");

        // 7b. Execute atomic database transaction rollback test directly at repository layer
        var writeRepo = verificationScope.ServiceProvider.GetRequiredService<IGoalBatchWriteRepository>();
        var rollbackGoal1Id = Guid.NewGuid();
        var rollbackGoal2Id = Guid.NewGuid();

        var failingDbBatch = new List<Goal>
        {
            new()
            {
                Id = rollbackGoal1Id,
                EmployeeId = elenaId,
                Title = "Pre-failure Atomic Goal Candidate",
                Description = "This valid goal must be rolled back atomically when its sibling fails.",
                Category = GoalCategory.PROJECT,
                Priority = GoalPriority.HIGH,
                Status = GoalStatus.ACTIVE,
                Provenance = GoalProvenance.MANUAL,
                SourceTranscriptHash = "rollback-test-hash"
            },
            new()
            {
                Id = rollbackGoal2Id,
                EmployeeId = elenaId,
                Title = "", // Violates database entity constraint: empty title throws ArgumentException in SaveGoalsBatchAsync
                Description = "Faulty sibling goal",
                Category = GoalCategory.TECHNICAL,
                Priority = GoalPriority.LOW,
                Status = GoalStatus.ACTIVE,
                Provenance = GoalProvenance.MANUAL
            }
        };

        var act = async () => await writeRepo.SaveGoalsBatchAsync(failingDbBatch);
        await act.Should().ThrowAsync<ArgumentException>();

        // Assert that rollbackGoal1Id was rolled back and is NOT present in the database
        var rolledBackGoal1 = await verificationDb.Goals.FindAsync(rollbackGoal1Id);
        rolledBackGoal1.Should().BeNull("Atomic database transaction rollback must eliminate partially inserted records");

        var finalDbCount = await verificationDb.Goals.AsNoTracking().CountAsync();
        finalDbCount.Should().Be(countBeforeRollback,
            "Total database count must remain completely identical before and after transactional rollback");
    }
}
