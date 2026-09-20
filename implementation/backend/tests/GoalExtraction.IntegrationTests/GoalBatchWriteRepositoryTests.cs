namespace GoalExtraction.IntegrationTests;

using FluentAssertions;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using GoalExtraction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class GoalBatchWriteRepositoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GoalBatchWriteRepositoryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task SaveGoalsBatchAsync_ValidGoals_PersistsAllAtomically()
    {
        using var scope = _factory.Services.CreateScope();
        var writeRepo = scope.ServiceProvider.GetRequiredService<IGoalBatchWriteRepository>();
        var readRepo = scope.ServiceProvider.GetRequiredService<IReadOnlyGoalQueryRepository>();

        var employeeId = DatabaseInitializer.ElenaRostovaId;
        var batch = new List<Goal>
        {
            new()
            {
                EmployeeId = employeeId,
                Title = "Batch Goal 1: Microservice refactoring",
                Description = "Refactor monolithic components into clean isolated services.",
                Category = GoalCategory.TECHNICAL,
                Metric = "100% services isolated",
                Timeframe = "Q4 2026",
                Priority = GoalPriority.HIGH,
                Status = GoalStatus.ACTIVE,
                Provenance = GoalProvenance.AI_ORIGINAL,
                SourceTranscriptHash = "hash123"
            },
            new()
            {
                EmployeeId = employeeId,
                Title = "Batch Goal 2: Mentoring junior engineers",
                Description = "Conduct bi-weekly pair programming sessions.",
                Category = GoalCategory.DEVELOPMENT,
                Metric = "2 sessions per sprint",
                Timeframe = "H2 2026",
                Priority = GoalPriority.MEDIUM,
                Status = GoalStatus.ACTIVE,
                Provenance = GoalProvenance.AI_ORIGINAL,
                SourceTranscriptHash = "hash123"
            },
            new()
            {
                EmployeeId = employeeId,
                Title = "Batch Goal 3: Zero security vulnerabilities",
                Description = "Resolve all Dependabot alerts and critical static analysis warnings.",
                Category = GoalCategory.PROJECT,
                Metric = "0 open critical CVEs",
                Timeframe = "Continuous",
                Priority = GoalPriority.HIGH,
                Status = GoalStatus.ACTIVE,
                Provenance = GoalProvenance.MANUAL,
                SourceTranscriptHash = "hash123"
            }
        };

        var savedIds = await writeRepo.SaveGoalsBatchAsync(batch);

        savedIds.Should().HaveCount(3);
        savedIds.Should().OnlyHaveUniqueItems();

        var elenaGoals = await readRepo.GetGoalsByEmployeeIdAsync(employeeId, limit: 50);
        var elenaGoalIds = elenaGoals.Select(g => g.Id).ToList();
        foreach (var id in savedIds)
        {
            elenaGoalIds.Should().Contain(id);
        }
    }

    [Fact]
    public async Task SaveGoalsBatchAsync_ConstraintViolation_RollsBackEntireBatch()
    {
        using var scope = _factory.Services.CreateScope();
        var writeRepo = scope.ServiceProvider.GetRequiredService<IGoalBatchWriteRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<GoalExtractionDbContext>();

        var employeeId = DatabaseInitializer.ElenaRostovaId;
        var initialGoalCount = await dbContext.Goals.CountAsync();

        var goal1Id = Guid.NewGuid();
        var goal2Id = Guid.NewGuid();

        var invalidBatch = new List<Goal>
        {
            new()
            {
                Id = goal1Id,
                EmployeeId = employeeId,
                Title = "Valid Goal 1",
                Description = "Description 1",
                Category = GoalCategory.TECHNICAL
            },
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                Title = "", // Violates constraint (empty title)
                Description = "Invalid Goal without title",
                Category = GoalCategory.DEVELOPMENT
            },
            new()
            {
                Id = goal2Id,
                EmployeeId = employeeId,
                Title = "Valid Goal 2",
                Description = "Description 2",
                Category = GoalCategory.PROJECT
            }
        };

        var act = async () => await writeRepo.SaveGoalsBatchAsync(invalidBatch);

        await act.Should().ThrowAsync<Exception>();

        // Verify atomic rollback: neither goal1 nor goal2 was persisted
        var finalGoalCount = await dbContext.Goals.CountAsync();
        finalGoalCount.Should().Be(initialGoalCount, "zero records should be inserted when batch fails");

        var goal1InDb = await dbContext.Goals.FindAsync(goal1Id);
        goal1InDb.Should().BeNull("Goal 1 should have been rolled back");

        var goal2InDb = await dbContext.Goals.FindAsync(goal2Id);
        goal2InDb.Should().BeNull("Goal 2 should have been rolled back");
    }

    [Fact]
    public async Task SaveGoalsBatchAsync_TitleLengthConstraintViolation_RollsBackEntireBatch()
    {
        using var scope = _factory.Services.CreateScope();
        var writeRepo = scope.ServiceProvider.GetRequiredService<IGoalBatchWriteRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<GoalExtractionDbContext>();

        var employeeId = DatabaseInitializer.ElenaRostovaId;
        var initialGoalCount = await dbContext.Goals.CountAsync();

        var goal1Id = Guid.NewGuid();
        var goal2Id = Guid.NewGuid();

        var batchWithTooLongTitle = new List<Goal>
        {
            new()
            {
                Id = goal1Id,
                EmployeeId = employeeId,
                Title = "Legitimate Title",
                Description = "Valid Description",
                Category = GoalCategory.TECHNICAL
            },
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                Title = new string('X', 121), // Exceeds 120 character limit
                Description = "Invalid Goal exceeding title limit",
                Category = GoalCategory.DEVELOPMENT
            },
            new()
            {
                Id = goal2Id,
                EmployeeId = employeeId,
                Title = "Another Legitimate Title",
                Description = "Valid Description 2",
                Category = GoalCategory.PROJECT
            }
        };

        var act = async () => await writeRepo.SaveGoalsBatchAsync(batchWithTooLongTitle);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceeds maximum length of 120 characters*");

        var finalGoalCount = await dbContext.Goals.CountAsync();
        finalGoalCount.Should().Be(initialGoalCount, "database count must remain unchanged after rollback");

        var goal1InDb = await dbContext.Goals.FindAsync(goal1Id);
        goal1InDb.Should().BeNull();

        var goal2InDb = await dbContext.Goals.FindAsync(goal2Id);
        goal2InDb.Should().BeNull();
    }
}
