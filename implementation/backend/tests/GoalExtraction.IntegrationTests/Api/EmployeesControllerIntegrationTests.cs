namespace GoalExtraction.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GoalExtraction.Api.Middleware;
using GoalExtraction.Application.DTOs;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class EmployeesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EmployeesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.CreateClient(); // Initialize DB and seed
    }

    [Fact]
    public async Task GetEmployees_Returns200OK_WithSeededEmployees()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>();
        employees.Should().NotBeNull();
        employees!.Count.Should().BeGreaterOrEqualTo(2);
        employees.Should().Contain(e => e.FullName == "Elena Rostova" && e.Department == "Software Engineering");
        employees.Should().Contain(e => e.FullName == "Marcus Vance" && e.Department == "Engineering Management");
    }

    [Fact]
    public async Task GetEmployeeGoals_WithValidEmployeeId_Returns200OK_WithActiveGoals()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/employees/{DatabaseInitializer.ElenaRostovaId}/goals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        var goals = await response.Content.ReadFromJsonAsync<List<Goal>>(jsonOptions);
        goals.Should().NotBeNull();
        goals!.Should().NotBeEmpty();
        goals.Should().OnlyContain(g => g.EmployeeId == DatabaseInitializer.ElenaRostovaId);
        goals.Should().OnlyContain(g => g.Status == GoalStatus.ACTIVE);
    }

    [Fact]
    public async Task GetEmployeeGoals_WithNonExistentId_Returns404NotFound_WithProblemDetails()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/employees/{nonExistentId}/goals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName).Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Title.Should().Be("Not Found");
        problem.Detail.Should().Contain(nonExistentId.ToString());
    }
}
