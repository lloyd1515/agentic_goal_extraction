namespace GoalExtraction.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

public class ReadOnlyGoalExtractionDbContext : GoalExtractionDbContext
{
    public ReadOnlyGoalExtractionDbContext(DbContextOptions<ReadOnlyGoalExtractionDbContext> options)
        : base(options)
    {
    }

    public override int SaveChanges()
    {
        throw new InvalidOperationException("ReadOnlyGoalExtractionDbContext is read-only and does not permit write operations.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new InvalidOperationException("ReadOnlyGoalExtractionDbContext is read-only and does not permit write operations.");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("ReadOnlyGoalExtractionDbContext is read-only and does not permit write operations.");
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("ReadOnlyGoalExtractionDbContext is read-only and does not permit write operations.");
    }
}
