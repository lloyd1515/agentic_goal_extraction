namespace GoalExtraction.Infrastructure.Persistence.Repositories;

using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Interfaces;
using GoalExtraction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class GoalBatchWriteRepository : IGoalBatchWriteRepository
{
    private readonly GoalExtractionDbContext _dbContext;

    public GoalBatchWriteRepository(GoalExtractionDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<Guid>> SaveGoalsBatchAsync(
        IReadOnlyList<Goal> goals, 
        CancellationToken ct = default)
    {
        if (goals == null || goals.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var savedIds = new List<Guid>(goals.Count);

            foreach (var goal in goals)
            {
                if (goal.Id == Guid.Empty)
                {
                    goal.Id = Guid.NewGuid();
                }

                if (goal.CreatedAt == default)
                {
                    goal.CreatedAt = DateTimeOffset.UtcNow;
                }

                if (string.IsNullOrWhiteSpace(goal.Title))
                {
                    throw new ArgumentException("Goal Title cannot be null or whitespace.", nameof(goals));
                }

                if (goal.Title.Length > 120)
                {
                    throw new ArgumentException($"Goal Title exceeds maximum length of 120 characters: {goal.Title.Length}", nameof(goals));
                }

                if (goal.Description != null && goal.Description.Length > 1000)
                {
                    throw new ArgumentException($"Goal Description exceeds maximum length of 1000 characters: {goal.Description.Length}", nameof(goals));
                }

                if (goal.EmployeeId == Guid.Empty)
                {
                    throw new ArgumentException("Goal EmployeeId cannot be empty.", nameof(goals));
                }

                // Prevent EF Core from re-inserting attached employee graph
                goal.Employee = null;

                await _dbContext.Goals.AddAsync(goal, ct);
                savedIds.Add(goal.Id);
            }

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return savedIds;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
