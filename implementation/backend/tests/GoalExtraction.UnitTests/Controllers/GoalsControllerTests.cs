namespace GoalExtraction.UnitTests.Controllers;

using System.Reflection;
using FluentAssertions;
using GoalExtraction.Api.Controllers;
using GoalExtraction.Api.Middleware;
using GoalExtraction.Application.Commands.ExtractGoals;
using GoalExtraction.Application.Commands.SaveGoalsBatch;
using GoalExtraction.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class GoalsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly GoalsController _controller;

    public GoalsControllerTests()
    {
        _controller = new GoalsController(_mediatorMock.Object);
    }

    [Fact]
    public void GoalsController_MustNeverDependOnDatabaseOrLlmClients()
    {
        var ctors = typeof(GoalsController).GetConstructors();
        ctors.Should().NotBeEmpty();

        foreach (var ctor in ctors)
        {
            var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();
            paramTypes.Should().OnlyContain(t => typeof(IMediator).IsAssignableFrom(t),
                "GoalsController must solely inject IMediator adhering to Single Responsibility and safety invariants.");
        }

        var fields = typeof(GoalsController).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            field.FieldType.Name.Should().NotContain("Repository");
            field.FieldType.Name.Should().NotContain("DbContext");
            field.FieldType.Name.Should().NotContain("Agent");
            field.FieldType.Name.Should().NotContain("Gemini");
        }
    }

    [Fact]
    public async Task ExtractGoals_DispatchesExtractGoalsCommand_ReturnsOkResult()
    {
        // Arrange
        const string correlationId = "test-corr-extract-999";
        var httpContext = new DefaultHttpContext();
        httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var requestDto = new ExtractGoalsRequestDto
        {
            Transcript = "Elena will migrate the legacy auth system by Q3 2026.",
            EmployeeId = Guid.NewGuid(),
            Department = "Software Engineering"
        };

        var expectedResult = new GoalExtractionResultDto
        {
            Summary = "Identified 1 technical goal.",
            Goals = new List<ProposedGoalDto>
            {
                new() { Title = "Migrate legacy auth", Category = "TECHNICAL", Priority = "HIGH" }
            },
            TranscriptHash = "hash-12345"
        };

        _mediatorMock.Setup(m => m.Send(
                It.Is<ExtractGoalsCommand>(cmd =>
                    cmd.Transcript == requestDto.Transcript &&
                    cmd.EmployeeId == requestDto.EmployeeId &&
                    cmd.Department == requestDto.Department &&
                    cmd.CorrelationId == correlationId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _controller.ExtractGoals(requestDto, CancellationToken.None);

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult.Result!;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task SaveGoalsBatch_DispatchesSaveGoalsBatchCommand_Returns201Created()
    {
        // Arrange
        const string correlationId = "test-corr-save-888";
        var httpContext = new DefaultHttpContext();
        httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var requestDto = new SaveGoalsBatchRequestDto
        {
            EmployeeId = employeeId,
            ReviewerId = reviewerId,
            SourceTranscriptHash = "trans-hash-888",
            Goals = new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Refactor payment service",
                    Description = "Migrate to stripe webhook idempotency",
                    Category = "TECHNICAL",
                    Metric = "Zero double charges",
                    Timeframe = "Q4 2026",
                    Priority = "HIGH",
                    Provenance = "AI_EDITED"
                }
            }
        };

        var createdGoalId = Guid.NewGuid();
        var expectedResult = new SaveGoalsBatchResultDto
        {
            Success = true,
            SavedCount = 1,
            SavedGoalIds = new List<Guid> { createdGoalId },
            SavedAt = DateTimeOffset.UtcNow
        };

        _mediatorMock.Setup(m => m.Send(
                It.Is<SaveGoalsBatchCommand>(cmd =>
                    cmd.EmployeeId == employeeId &&
                    cmd.ReviewerId == reviewerId &&
                    cmd.SourceTranscriptHash == "trans-hash-888" &&
                    cmd.CorrelationId == correlationId &&
                    cmd.Goals.Count == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _controller.SaveGoalsBatch(requestDto, CancellationToken.None);

        // Assert
        actionResult.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult.Result!;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().BeEquivalentTo(expectedResult);
    }
}
