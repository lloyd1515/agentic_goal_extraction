namespace GoalExtraction.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GoalExtraction.Api.Middleware;
using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Application.DTOs;
using GoalExtraction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

public class GoalsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GoalsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithMockedAgent()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing IGoalExtractionAgent registration
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IGoalExtractionAgent));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var mockAgent = new Mock<IGoalExtractionAgent>();
                mockAgent.Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ExtractedGoalsResult
                    {
                        Summary = "Extracted 1 technical goal from transcript.",
                        Goals = new List<ExtractedGoalItem>
                        {
                            new()
                            {
                                Title = "Deploy OAuth 2.1 auth gateway",
                                Description = "Upgrade identity services to OAuth 2.1 RFC specifications.",
                                Category = "TECHNICAL",
                                Metric = "Zero downtime during migration",
                                Timeframe = "Q3 2026",
                                Priority = "HIGH"
                            }
                        },
                        ToolCallsExecuted = new List<string> { "query_existing_employee_goals" }
                    });

                services.AddScoped(_ => mockAgent.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Extract_InvalidShortTranscript_Returns400BadRequest_WithProblemDetails()
    {
        // Arrange
        var client = CreateClientWithMockedAgent();
        var request = new ExtractGoalsRequestDto
        {
            Transcript = "Too short",
            EmployeeId = DatabaseInitializer.ElenaRostovaId
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/goals/extract", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Title.Should().Be("Validation Error");
        problem.Extensions.Should().ContainKey("errors");

        var errorsJson = JsonSerializer.Serialize(problem.Extensions["errors"]);
        errorsJson.Should().Contain("Transcript");
    }

    [Fact]
    public async Task Extract_ValidRequest_Returns200OK_AndIncludesCorrelationIdHeader()
    {
        // Arrange
        var client = CreateClientWithMockedAgent();
        var request = new ExtractGoalsRequestDto
        {
            Transcript = "In today's 1:1 meeting, Elena confirmed she will migrate all internal identity services to OAuth 2.1 by Q3 2026.",
            EmployeeId = DatabaseInitializer.ElenaRostovaId,
            Department = "Software Engineering"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/goals/extract", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();
        var correlationId = response.Headers.GetValues(CorrelationIdMiddleware.CorrelationIdHeaderName).FirstOrDefault();
        correlationId.Should().NotBeNullOrWhiteSpace();

        var result = await response.Content.ReadFromJsonAsync<GoalExtractionResultDto>();
        result.Should().NotBeNull();
        result!.Goals.Should().NotBeEmpty();
        result.Goals[0].Title.Should().Be("Deploy OAuth 2.1 auth gateway");
        result.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task Extract_WithCustomCorrelationId_EchoesSameCorrelationId()
    {
        // Arrange
        var client = CreateClientWithMockedAgent();
        const string customId = "custom-client-cid-555";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/goals/extract")
        {
            Content = JsonContent.Create(new ExtractGoalsRequestDto
            {
                Transcript = "Let us ensure that our test coverage reaches 85% by the end of Q4 2026.",
                EmployeeId = DatabaseInitializer.ElenaRostovaId
            })
        };
        httpRequest.Headers.Add(CorrelationIdMiddleware.CorrelationIdHeaderName, customId);

        // Act
        var response = await client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();
        response.Headers.GetValues(CorrelationIdMiddleware.CorrelationIdHeaderName).First().Should().Be(customId);

        var result = await response.Content.ReadFromJsonAsync<GoalExtractionResultDto>();
        result!.CorrelationId.Should().Be(customId);
    }

    [Fact]
    public async Task BatchSave_EmptyGoalsArray_Returns400BadRequest()
    {
        // Arrange
        var client = CreateClientWithMockedAgent();
        var request = new SaveGoalsBatchRequestDto
        {
            EmployeeId = DatabaseInitializer.ElenaRostovaId,
            Goals = new List<SaveGoalItemDto>()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/goals/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Extensions.Should().ContainKey("errors");
        var errorsJson = JsonSerializer.Serialize(problem.Extensions["errors"]);
        errorsJson.Should().Contain("Goals");
    }

    [Fact]
    public async Task BatchSave_InvalidEmptyEmployeeId_Returns400BadRequest()
    {
        // Arrange
        var client = CreateClientWithMockedAgent();
        var request = new SaveGoalsBatchRequestDto
        {
            EmployeeId = Guid.Empty,
            Goals = new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Deliver Q3 Milestones",
                    Description = "Ensure on-time delivery of the search index microservice.",
                    Category = "PERFORMANCE",
                    Priority = "HIGH"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/goals/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Extensions.Should().ContainKey("errors");
        var errorsJson = JsonSerializer.Serialize(problem.Extensions["errors"]);
        errorsJson.Should().Contain("EmployeeId");
    }

    [Fact]
    public async Task BatchSave_ValidGoals_Returns201Created()
    {
        // Arrange
        var client = CreateClientWithMockedAgent();
        var request = new SaveGoalsBatchRequestDto
        {
            EmployeeId = DatabaseInitializer.ElenaRostovaId,
            ReviewerId = DatabaseInitializer.MarcusVanceId,
            SourceTranscriptHash = "0000000000000000000000000000000000000000000000000000000000000009",
            Goals = new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Implement Distributed Cache Layer",
                    Description = "Introduce Redis distributed caching for read-heavy catalog endpoints.",
                    Category = "TECHNICAL",
                    Metric = "P99 latency < 50ms",
                    Timeframe = "Q4 2026",
                    Priority = "HIGH",
                    Provenance = "AI_ORIGINAL"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/goals/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var result = await response.Content.ReadFromJsonAsync<SaveGoalsBatchResultDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.SavedCount.Should().Be(1);
        result.SavedGoalIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task BatchSave_NonExistentEmployee_Returns404NotFound()
    {
        // Arrange
        var client = CreateClientWithMockedAgent();
        var nonExistentId = Guid.NewGuid();
        var request = new SaveGoalsBatchRequestDto
        {
            EmployeeId = nonExistentId,
            Goals = new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Valid Goal Title",
                    Description = "Valid Goal Description",
                    Category = "TECHNICAL",
                    Priority = "HIGH",
                    Provenance = "AI_ORIGINAL"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/goals/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Title.Should().Be("Not Found");
    }

    [Fact]
    public async Task SwaggerEndpoint_Returns200OK_WithCompleteOpenApiSpec()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        paths.TryGetProperty("/api/goals/extract", out var extractPath).Should().BeTrue();
        extractPath.TryGetProperty("post", out _).Should().BeTrue();

        paths.TryGetProperty("/api/goals/batch", out var batchPath).Should().BeTrue();
        batchPath.TryGetProperty("post", out _).Should().BeTrue();

        paths.TryGetProperty("/api/employees", out var employeesPath).Should().BeTrue();
        employeesPath.TryGetProperty("get", out _).Should().BeTrue();

        paths.TryGetProperty("/api/employees/{id}/goals", out var empGoalsPath).Should().BeTrue();
        empGoalsPath.TryGetProperty("get", out _).Should().BeTrue();
    }
}
