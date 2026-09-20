namespace GoalExtraction.Domain.Interfaces;

using GoalExtraction.Domain.Entities;

public interface IGoalBatchWriteRepository
{
    Task<IReadOnlyList<Guid>> SaveGoalsBatchAsync(
        IReadOnlyList<Goal> goals, 
        CancellationToken ct = default);
}
