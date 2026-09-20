namespace GoalExtraction.UnitTests.Controllers;

using FluentAssertions;
using GoalExtraction.Api.Controllers;
using GoalExtraction.Application.Common;
using GoalExtraction.Application.DTOs;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class EmployeesControllerTests
{
    private readonly Mock<IReadOnlyGoalQueryRepository> _queryRepoMock = new();
    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        _controller = new EmployeesController(_queryRepoMock.Object);
    }

    [Fact]
    public async Task GetEmployees_ReturnsMappedEmployeeDtos()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FullName = "Elena Rostova",
                Email = "elena@example.com",
                Department = "Software Engineering"
            },
            new()
            {
                Id = Guid.NewGuid(),
                FullName = "Marcus Vance",
                Email = "marcus@example.com",
                Department = "Engineering Management"
            }
        };

        _queryRepoMock.Setup(r => r.GetAllEmployeesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        // Act
        var result = await _controller.GetEmployees(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var dtos = okResult.Value as IReadOnlyList<EmployeeDto>;
        dtos.Should().NotBeNull();
        dtos!.Count.Should().Be(2);
        dtos[0].FullName.Should().Be("Elena Rostova");
        dtos[1].FullName.Should().Be("Marcus Vance");
    }

    [Fact]
    public async Task GetEmployeeGoals_WhenEmployeeExists_ReturnsActiveGoals()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            FullName = "Elena Rostova",
            Email = "elena@example.com",
            Department = "Software Engineering"
        };

        var goals = new List<Goal>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                Title = "Improve performance",
                Description = "Optimize SQL queries",
                Status = GoalStatus.ACTIVE
            }
        };

        _queryRepoMock.Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _queryRepoMock.Setup(r => r.GetGoalsByEmployeeIdAsync(employeeId, GoalStatus.ACTIVE, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goals);

        // Act
        var result = await _controller.GetEmployeeGoals(employeeId, 20, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var returnedGoals = okResult.Value as IReadOnlyList<Goal>;
        returnedGoals.Should().NotBeNull();
        returnedGoals!.Count.Should().Be(1);
        returnedGoals[0].Title.Should().Be("Improve performance");
    }

    [Fact]
    public async Task GetEmployeeGoals_WhenEmployeeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _queryRepoMock.Setup(r => r.GetEmployeeByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var act = () => _controller.GetEmployeeGoals(employeeId, 20, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{employeeId}*");
    }
}
