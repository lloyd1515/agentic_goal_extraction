namespace GoalExtraction.UnitTests.AI;

using System.Text.Json;
using FluentAssertions;
using GoalExtraction.Application.AI.Tools;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using Moq;
using Xunit;

public class ReadOnlyGoalQueryToolTests
{
    private readonly Mock<IReadOnlyGoalQueryRepository> _mockRepo;
    private readonly ReadOnlyGoalQueryTool _tool;

    public ReadOnlyGoalQueryToolTests()
    {
        _mockRepo = new Mock<IReadOnlyGoalQueryRepository>();
        _tool = new ReadOnlyGoalQueryTool(_mockRepo.Object);
    }

    [Fact]
    public void GetToolDeclaration_ReturnsValidDeclarationObject()
    {
        var declaration = _tool.GetToolDeclaration();
        declaration.Should().NotBeNull();
        _tool.ToolName.Should().Be("query_existing_employee_goals");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidEmployeeId_QueriesRepositoryAndReturnsGoalsJson()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            FullName = "Elena Rostova",
            Email = "elena.rostova@example.com",
            Department = "Engineering"
        };

        var goals = new List<Goal>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                Title = "Improve test coverage",
                Description = "Bring coverage to 75%",
                Category = GoalCategory.TECHNICAL,
                Metric = "75% coverage",
                Timeframe = "Q4",
                Priority = GoalPriority.HIGH,
                Status = GoalStatus.ACTIVE,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockRepo.Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(employee);

        _mockRepo.Setup(r => r.GetGoalsByEmployeeIdAsync(employeeId, GoalStatus.ACTIVE, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(goals);

        var args = JsonSerializer.Serialize(new { employee_id = employeeId.ToString() });

        // Act
        var resultJson = await _tool.ExecuteAsync(args);

        // Assert
        resultJson.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        root.GetProperty("count").GetInt32().Should().Be(1);
        root.GetProperty("employee").GetProperty("name").GetString().Should().Be("Elena Rostova");
        root.GetProperty("goals").GetArrayLength().Should().Be(1);
        root.GetProperty("goals")[0].GetProperty("title").GetString().Should().Be("Improve test coverage");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmployeeName_QueriesRepositoryAndReturnsGoals()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            FullName = "Marcus Vance",
            Department = "Product"
        };

        _mockRepo.Setup(r => r.GetEmployeeByNameAsync("Marcus Vance", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(employee);

        _mockRepo.Setup(r => r.GetGoalsByEmployeeIdAsync(employeeId, GoalStatus.ACTIVE, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Goal>());

        var args = JsonSerializer.Serialize(new { employee_name = "Marcus Vance" });

        // Act
        var resultJson = await _tool.ExecuteAsync(args);

        // Assert
        resultJson.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        root.GetProperty("count").GetInt32().Should().Be(0);
        root.GetProperty("employee").GetProperty("name").GetString().Should().Be("Marcus Vance");
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrows_ReturnsFallbackErrorJson()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("DB connection timed out"));

        var args = JsonSerializer.Serialize(new { employee_id = employeeId.ToString() });

        // Act
        var resultJson = await _tool.ExecuteAsync(args);

        // Assert
        resultJson.Should().Be("{\"error\": \"Context database unavailable\"}");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoEmployeeContextProvided_ReturnsInformativePayload()
    {
        // Arrange
        var args = "{}";

        // Act
        var resultJson = await _tool.ExecuteAsync(args);

        // Assert
        resultJson.Should().Contain("No employee ID or employee name specified");
    }
}
