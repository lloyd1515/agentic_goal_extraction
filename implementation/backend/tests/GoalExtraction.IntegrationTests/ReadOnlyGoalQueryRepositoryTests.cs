namespace GoalExtraction.IntegrationTests;

using FluentAssertions;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using GoalExtraction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ReadOnlyGoalQueryRepositoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReadOnlyGoalQueryRepositoryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_WithValidId_ReturnsEmployee()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryRepository>();

        var employee = await repo.GetEmployeeByIdAsync(DatabaseInitializer.ElenaRostovaId);

        employee.Should().NotBeNull();
        employee!.FullName.Should().Be("Elena Rostova");
        employee.Department.Should().Be("Software Engineering");
    }

    [Fact]
    public async Task GetEmployeeByNameAsync_WithFullName_ReturnsMatch()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryRepository>();

        var employee = await repo.GetEmployeeByNameAsync("Elena Rostova");

        employee.Should().NotBeNull();
        employee!.Id.Should().Be(DatabaseInitializer.ElenaRostovaId);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_WithMarcusVanceId_ReturnsMarcusVance()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryRepository>();

        var employee = await repo.GetEmployeeByIdAsync(DatabaseInitializer.MarcusVanceId);

        employee.Should().NotBeNull();
        employee!.FullName.Should().Be("Marcus Vance");
        employee.Department.Should().Be("Engineering Management");
    }

    [Fact]
    public async Task GetEmployeeByNameAsync_WithMarcusVanceFullName_ReturnsMatch()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryRepository>();

        var employee = await repo.GetEmployeeByNameAsync("Marcus Vance");

        employee.Should().NotBeNull();
        employee!.Id.Should().Be(DatabaseInitializer.MarcusVanceId);
        employee.FullName.Should().Be("Marcus Vance");
    }

    [Fact]
    public async Task GetEmployeeByNameAsync_WithPartialName_ReturnsMatch()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryRepository>();

        var employee = await repo.GetEmployeeByNameAsync("Marcus");

        employee.Should().NotBeNull();
        employee!.FullName.Should().Be("Marcus Vance");
    }

    [Fact]
    public async Task GetGoalsByEmployeeIdAsync_ReturnsActiveGoals()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryRepository>();

        var goals = await repo.GetGoalsByEmployeeIdAsync(DatabaseInitializer.ElenaRostovaId, GoalStatus.ACTIVE);

        goals.Should().NotBeNull();
        goals.Should().NotBeEmpty();
        goals.Should().OnlyContain(g => g.EmployeeId == DatabaseInitializer.ElenaRostovaId && g.Status == GoalStatus.ACTIVE);
    }

    [Fact]
    public async Task SearchGoalsAsync_MatchesByTitleOrContent()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryRepository>();

        var results = await repo.SearchGoalsAsync("OAuth");

        results.Should().NotBeNull();
        results.Should().NotBeEmpty();
        results.Should().Contain(g => g.Title.Contains("OAuth"));
    }
}
