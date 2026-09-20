namespace GoalExtraction.UnitTests.Commands;

using FluentAssertions;
using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Application.Commands.ExtractGoals;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Interfaces;
using Moq;
using Xunit;

public class ExtractGoalsCommandHandlerTests
{
    private readonly Mock<IGoalExtractionAgent> _agentMock = new();
    private readonly Mock<IReadOnlyGoalQueryRepository> _queryRepoMock = new();

    [Fact]
    public async Task Handle_ValidCommand_SanitizesTranscript_CallsAgent_ReturnsMappedResult()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            FullName = "Jane Doe",
            Department = "Engineering",
            Email = "jane@example.com"
        };

        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var agentResult = new ExtractedGoalsResult
        {
            Summary = "Quarterly goals focused on system reliability.",
            Goals = new List<ExtractedGoalItem>
            {
                new()
                {
                    Title = "Achieve 99.99% service availability",
                    Description = "Deploy redundant regional workers and automated failover.",
                    Category = "PERFORMANCE",
                    Metric = "99.99% uptime",
                    Timeframe = "Q4 2026",
                    Priority = "HIGH"
                }
            },
            ToolCallsExecuted = new List<string> { "search_existing_goals" },
            Success = true
        };

        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agentResult);

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var rawTranscript = "Jane: Let's focus on service availability.\u0000\u0007\r\nWe need 99.99% uptime!";
        var command = new ExtractGoalsCommand(
            Transcript: rawTranscript,
            EmployeeId: employeeId,
            Department: "Engineering",
            CorrelationId: "test-corr-456");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CorrelationId.Should().Be("test-corr-456");
        result.Summary.Should().Be("Quarterly goals focused on system reliability.");
        result.Goals.Should().HaveCount(1);
        result.Goals[0].Title.Should().Be("Achieve 99.99% service availability");
        result.Goals[0].TempId.Should().NotBeEmpty();
        result.Goals[0].Category.Should().Be("PERFORMANCE");
        result.Goals[0].Priority.Should().Be("HIGH");
        result.Goals[0].Provenance.Should().Be("AI_ORIGINAL");
        result.ToolCallsExecuted.Should().ContainSingle().Which.Should().Be("search_existing_goals");
        result.TranscriptHash.Should().NotBeNullOrWhiteSpace();

        // Verify context passed to agent had sanitized transcript and employee name
        _agentMock.Verify(a => a.ExtractGoalsAsync(
            It.Is<AgentExtractionContext>(ctx =>
                ctx.EmployeeName == "Jane Doe" &&
                ctx.EmployeeId == employeeId &&
                !ctx.Transcript.Contains("\u0000")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutEmployeeId_ProceedsWithoutEmployeeLookup()
    {
        // Arrange
        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult { Summary = "Generic summary" });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "This is a valid meeting transcript with sufficient characters.",
            EmployeeId: null,
            CorrelationId: "no-emp-corr");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _queryRepoMock.Verify(r => r.GetEmployeeByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _agentMock.Verify(a => a.ExtractGoalsAsync(
            It.Is<AgentExtractionContext>(ctx => ctx.EmployeeName == null && ctx.EmployeeId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullAgent_ThrowsArgumentNullException()
    {
        var act = () => new ExtractGoalsCommandHandler(null!, _queryRepoMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("agent");
    }

    [Fact]
    public void Constructor_NullQueryRepository_ThrowsArgumentNullException()
    {
        var act = () => new ExtractGoalsCommandHandler(_agentMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("queryRepository");
    }

    [Fact]
    public async Task Handle_EmployeeNotFoundInRepo_PassesNullEmployeeNameToAgent()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult { Success = true, Summary = "Summary with missing employee" });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            EmployeeId: employeeId,
            CorrelationId: "missing-emp-corr");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _agentMock.Verify(a => a.ExtractGoalsAsync(
            It.Is<AgentExtractionContext>(ctx => ctx.EmployeeName == null && ctx.EmployeeId == employeeId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmployeeWithEmptyOrNullName_PassesNullOrEmptyNameToAgent(string? employeeName)
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = employeeId, FullName = employeeName! });

        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult { Success = true, Summary = "Summary" });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            EmployeeId: employeeId,
            CorrelationId: "empty-name-corr");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _agentMock.Verify(a => a.ExtractGoalsAsync(
            It.Is<AgentExtractionContext>(ctx => ctx.EmployeeName == employeeName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmployeeIdIsEmptyGuid_DoesNotQueryRepository()
    {
        // Arrange
        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult { Success = true, Summary = "Empty Guid test" });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            EmployeeId: Guid.Empty,
            CorrelationId: "empty-guid-corr");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _queryRepoMock.Verify(r => r.GetEmployeeByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _agentMock.Verify(a => a.ExtractGoalsAsync(
            It.Is<AgentExtractionContext>(ctx => ctx.EmployeeName == null && ctx.EmployeeId == Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentReturnsFailure_WithExplicitErrorMessage_ThrowsInvalidOperationException()
    {
        // Arrange
        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult
            {
                Success = false,
                ErrorMessage = "Rate limit exceeded by upstream AI service."
            });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            CorrelationId: "agent-failure-corr");

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Rate limit exceeded by upstream AI service.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_AgentReturnsFailure_WithNullOrWhitespaceErrorMessage_ThrowsDefaultInvalidOperationException(string? errorMessage)
    {
        // Arrange
        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult
            {
                Success = false,
                ErrorMessage = errorMessage
            });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            CorrelationId: "agent-failure-default-corr");

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AI goal extraction failed to produce valid results.");
    }

    [Fact]
    public async Task Handle_AgentReturnsNullGoals_MapsToEmptyGoalsList()
    {
        // Arrange
        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult
            {
                Success = true,
                Goals = null!,
                Summary = "No goals identified",
                ToolCallsExecuted = null!
            });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            CorrelationId: "null-goals-corr");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Goals.Should().NotBeNull().And.BeEmpty();
        result.Summary.Should().Be("No goals identified");
        result.ToolCallsExecuted.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Handle_AgentReturnsNullSummary_MapsToEmptyString()
    {
        // Arrange
        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult
            {
                Success = true,
                Summary = null!,
                Goals = new List<ExtractedGoalItem>()
            });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            CorrelationId: "null-summary-corr");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Handle_GoalItemsWithNullOrWhitespaceCategoryAndPriority_DefaultsToPerformanceAndMedium()
    {
        // Arrange
        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult
            {
                Success = true,
                Summary = "Summary",
                Goals = new List<ExtractedGoalItem>
                {
                    new()
                    {
                        Title = null!,
                        Description = null!,
                        Category = null!,
                        Metric = null!,
                        Timeframe = null!,
                        Priority = "   "
                    }
                }
            });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            CorrelationId: "default-category-prio-corr");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Goals.Should().HaveCount(1);
        result.Goals[0].Title.Should().Be(string.Empty);
        result.Goals[0].Description.Should().Be(string.Empty);
        result.Goals[0].Category.Should().Be("PERFORMANCE");
        result.Goals[0].Metric.Should().Be(string.Empty);
        result.Goals[0].Timeframe.Should().Be(string.Empty);
        result.Goals[0].Priority.Should().Be("MEDIUM");
    }

    [Fact]
    public async Task Handle_GoalItemsWithLowercaseValues_NormalizesToUppercaseTrimmed()
    {
        // Arrange
        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedGoalsResult
            {
                Success = true,
                Summary = "Summary",
                Goals = new List<ExtractedGoalItem>
                {
                    new()
                    {
                        Title = "Custom title",
                        Category = "  development  ",
                        Priority = "  low  "
                    }
                }
            });

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            CorrelationId: "normalized-corr");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Goals.Should().HaveCount(1);
        result.Goals[0].Category.Should().Be("DEVELOPMENT");
        result.Goals[0].Priority.Should().Be("LOW");
    }

    [Fact]
    public async Task Handle_AgentThrowsException_BubblesUp()
    {
        // Arrange
        _agentMock
            .Setup(a => a.ExtractGoalsAsync(It.IsAny<AgentExtractionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Upstream LLM timeout"));

        var handler = new ExtractGoalsCommandHandler(_agentMock.Object, _queryRepoMock.Object);
        var command = new ExtractGoalsCommand(
            Transcript: "Valid meeting transcript with sufficient characters.",
            CorrelationId: "timeout-corr");

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<TimeoutException>().WithMessage("Upstream LLM timeout");
    }
}

