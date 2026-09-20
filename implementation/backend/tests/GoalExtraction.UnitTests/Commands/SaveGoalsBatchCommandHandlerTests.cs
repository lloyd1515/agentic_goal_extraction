namespace GoalExtraction.UnitTests.Commands;

using FluentAssertions;
using GoalExtraction.Application.Commands.SaveGoalsBatch;
using GoalExtraction.Application.Common;
using GoalExtraction.Application.DTOs;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using Moq;
using Xunit;

public class SaveGoalsBatchCommandHandlerTests
{
    private readonly Mock<IGoalBatchWriteRepository> _writeRepoMock = new();
    private readonly Mock<IReadOnlyGoalQueryRepository> _queryRepoMock = new();

    [Fact]
    public async Task Handle_EmployeeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var handler = new SaveGoalsBatchCommandHandler(_writeRepoMock.Object, _queryRepoMock.Object);
        var command = new SaveGoalsBatchCommand(
            EmployeeId: employeeId,
            SourceTranscriptHash: "hash123",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Test Goal",
                    Description = "Valid goal description for testing.",
                    Category = "PERFORMANCE",
                    Priority = "HIGH"
                }
            });

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{employeeId}*");

        _writeRepoMock.Verify(r => r.SaveGoalsBatchAsync(It.IsAny<IReadOnlyList<Goal>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_MapsEntitiesWithActiveStatus_PersistsBatchAtomically()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            FullName = "Alex Developer",
            Department = "Platform",
            Email = "alex@example.com"
        };

        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var generatedGoalId1 = Guid.NewGuid();
        var generatedGoalId2 = Guid.NewGuid();
        var expectedSavedIds = new List<Guid> { generatedGoalId1, generatedGoalId2 };

        IReadOnlyList<Goal>? capturedEntities = null;
        _writeRepoMock
            .Setup(r => r.SaveGoalsBatchAsync(It.IsAny<IReadOnlyList<Goal>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Goal>, CancellationToken>((goals, _) => capturedEntities = goals)
            .ReturnsAsync(expectedSavedIds);

        var handler = new SaveGoalsBatchCommandHandler(_writeRepoMock.Object, _queryRepoMock.Object);
        var command = new SaveGoalsBatchCommand(
            EmployeeId: employeeId,
            SourceTranscriptHash: "transcript-hash-abc",
            ReviewerId: reviewerId,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Ship Feature X",
                    Description = "Deliver Feature X according to specification.",
                    Category = "PROJECT",
                    Metric = "100% test coverage",
                    Timeframe = "Sprint 4",
                    Priority = "HIGH",
                    Provenance = "AI_MODIFIED"
                },
                new()
                {
                    Title = "Mentorship sessions",
                    Description = "Lead weekly architecture reviews with junior engineers.",
                    Category = "DEVELOPMENT",
                    Metric = "12 sessions",
                    Timeframe = "H2 2026",
                    Priority = "MEDIUM",
                    Provenance = "MANUAL"
                }
            },
            CorrelationId: "save-batch-corr-999");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("save-batch-corr-999");
        result.SavedCount.Should().Be(2);
        result.SavedGoalIds.Should().BeEquivalentTo(expectedSavedIds);

        capturedEntities.Should().NotBeNull();
        capturedEntities!.Count.Should().Be(2);

        // Verify entity properties
        capturedEntities[0].EmployeeId.Should().Be(employeeId);
        capturedEntities[0].Title.Should().Be("Ship Feature X");
        capturedEntities[0].Category.Should().Be(GoalCategory.PROJECT);
        capturedEntities[0].Priority.Should().Be(GoalPriority.HIGH);
        capturedEntities[0].Status.Should().Be(GoalStatus.ACTIVE);
        capturedEntities[0].Provenance.Should().Be(GoalProvenance.AI_MODIFIED);
        capturedEntities[0].SourceTranscriptHash.Should().Be("transcript-hash-abc");
        capturedEntities[0].ReviewerId.Should().Be(reviewerId);

        capturedEntities[1].EmployeeId.Should().Be(employeeId);
        capturedEntities[1].Title.Should().Be("Mentorship sessions");
        capturedEntities[1].Category.Should().Be(GoalCategory.DEVELOPMENT);
        capturedEntities[1].Priority.Should().Be(GoalPriority.MEDIUM);
        capturedEntities[1].Status.Should().Be(GoalStatus.ACTIVE);
        capturedEntities[1].Provenance.Should().Be(GoalProvenance.MANUAL);
    }

    [Fact]
    public void Constructor_NullWriteRepository_ThrowsArgumentNullException()
    {
        var act = () => new SaveGoalsBatchCommandHandler(null!, _queryRepoMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("writeRepository");
    }

    [Fact]
    public void Constructor_NullQueryRepository_ThrowsArgumentNullException()
    {
        var act = () => new SaveGoalsBatchCommandHandler(_writeRepoMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("queryRepository");
    }

    [Fact]
    public async Task Handle_EmptyGoalsList_PersistsZeroGoalsAndReturnsSuccess()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = employeeId, FullName = "Alex Developer" });

        IReadOnlyList<Goal>? capturedGoals = null;
        _writeRepoMock
            .Setup(r => r.SaveGoalsBatchAsync(It.IsAny<IReadOnlyList<Goal>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Goal>, CancellationToken>((goals, _) => capturedGoals = goals)
            .ReturnsAsync(new List<Guid>());

        var handler = new SaveGoalsBatchCommandHandler(_writeRepoMock.Object, _queryRepoMock.Object);
        var command = new SaveGoalsBatchCommand(
            EmployeeId: employeeId,
            SourceTranscriptHash: "hash-empty",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SavedCount.Should().Be(0);
        result.SavedGoalIds.Should().BeEmpty();
        capturedGoals.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Handle_NullGoalsList_PersistsZeroGoalsAndReturnsSuccess()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = employeeId, FullName = "Alex Developer" });

        IReadOnlyList<Goal>? capturedGoals = null;
        _writeRepoMock
            .Setup(r => r.SaveGoalsBatchAsync(It.IsAny<IReadOnlyList<Goal>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Goal>, CancellationToken>((goals, _) => capturedGoals = goals)
            .ReturnsAsync(new List<Guid>());

        var handler = new SaveGoalsBatchCommandHandler(_writeRepoMock.Object, _queryRepoMock.Object);
        var command = new SaveGoalsBatchCommand(
            EmployeeId: employeeId,
            SourceTranscriptHash: "hash-null",
            ReviewerId: null,
            Goals: null!);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SavedCount.Should().Be(0);
        result.SavedGoalIds.Should().BeEmpty();
        capturedGoals.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Handle_ReviewerIdNull_MapsReviewerIdAsNull()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = employeeId, FullName = "Alex Developer" });

        IReadOnlyList<Goal>? capturedGoals = null;
        _writeRepoMock
            .Setup(r => r.SaveGoalsBatchAsync(It.IsAny<IReadOnlyList<Goal>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Goal>, CancellationToken>((goals, _) => capturedGoals = goals)
            .ReturnsAsync(new List<Guid> { Guid.NewGuid() });

        var handler = new SaveGoalsBatchCommandHandler(_writeRepoMock.Object, _queryRepoMock.Object);
        var command = new SaveGoalsBatchCommand(
            EmployeeId: employeeId,
            SourceTranscriptHash: "hash-rev-null",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Goal with null reviewer",
                    Description = "Description",
                    Category = "PERFORMANCE",
                    Priority = "MEDIUM"
                }
            });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedGoals.Should().NotBeNull().And.HaveCount(1);
        capturedGoals![0].ReviewerId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InvalidEnums_FallsBackToDefaults()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = employeeId, FullName = "Alex Developer" });

        IReadOnlyList<Goal>? capturedGoals = null;
        _writeRepoMock
            .Setup(r => r.SaveGoalsBatchAsync(It.IsAny<IReadOnlyList<Goal>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Goal>, CancellationToken>((goals, _) => capturedGoals = goals)
            .ReturnsAsync(new List<Guid> { Guid.NewGuid() });

        var handler = new SaveGoalsBatchCommandHandler(_writeRepoMock.Object, _queryRepoMock.Object);
        var command = new SaveGoalsBatchCommand(
            EmployeeId: employeeId,
            SourceTranscriptHash: "hash-enums",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Goal with invalid enums",
                    Description = "Description",
                    Category = "UNKNOWN_CATEGORY",
                    Priority = "UNKNOWN_PRIORITY",
                    Provenance = "UNKNOWN_PROVENANCE"
                }
            });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedGoals.Should().NotBeNull().And.HaveCount(1);
        capturedGoals![0].Category.Should().Be(GoalCategory.PERFORMANCE);
        capturedGoals[0].Priority.Should().Be(GoalPriority.MEDIUM);
        capturedGoals[0].Provenance.Should().Be(GoalProvenance.AI_ORIGINAL);
    }

    [Fact]
    public async Task Handle_NullOptionalFields_MapsToEmptyStringsAndTrims()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryRepoMock
            .Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = employeeId, FullName = "Alex Developer" });

        IReadOnlyList<Goal>? capturedGoals = null;
        _writeRepoMock
            .Setup(r => r.SaveGoalsBatchAsync(It.IsAny<IReadOnlyList<Goal>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Goal>, CancellationToken>((goals, _) => capturedGoals = goals)
            .ReturnsAsync(new List<Guid> { Guid.NewGuid() });

        var handler = new SaveGoalsBatchCommandHandler(_writeRepoMock.Object, _queryRepoMock.Object);
        var command = new SaveGoalsBatchCommand(
            EmployeeId: employeeId,
            SourceTranscriptHash: null,
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "  Trimmed Title  ",
                    Description = "  Trimmed Description  ",
                    Metric = null!,
                    Timeframe = null!,
                    Category = "DEVELOPMENT",
                    Priority = "LOW"
                }
            });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedGoals.Should().NotBeNull().And.HaveCount(1);
        capturedGoals![0].Title.Should().Be("Trimmed Title");
        capturedGoals[0].Description.Should().Be("Trimmed Description");
        capturedGoals[0].Metric.Should().Be(string.Empty);
        capturedGoals[0].Timeframe.Should().Be(string.Empty);
        capturedGoals[0].SourceTranscriptHash.Should().Be(string.Empty);
    }
}

