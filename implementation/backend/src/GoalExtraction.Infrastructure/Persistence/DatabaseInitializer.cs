namespace GoalExtraction.Infrastructure.Persistence;

using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class DatabaseInitializer
{
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public static readonly Guid ElenaRostovaId = Guid.Parse("e0a1b2c3-d4e5-0000-0000-000000000001");
    public static readonly Guid MarcusVanceId = Guid.Parse("e0a1b2c3-d4e5-0000-0000-000000000002");

    private readonly GoalExtractionDbContext _dbContext;
    private readonly ILogger<DatabaseInitializer>? _logger;

    public DatabaseInitializer(GoalExtractionDbContext dbContext, ILogger<DatabaseInitializer>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _initLock.WaitAsync(ct);
        try
        {
            _logger?.LogInformation("Ensuring SQLite database and schema exist...");
            try
            {
                await _dbContext.Database.EnsureCreatedAsync(ct);
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("already exists"))
            {
                _logger?.LogWarning(ex, "Table already exists during EnsureCreatedAsync.");
            }

            if (!await _dbContext.Employees.AnyAsync(ct))
            {
            _logger?.LogInformation("Seeding database with default employees and goals...");

            var elena = new Employee
            {
                Id = ElenaRostovaId,
                FullName = "Elena Rostova",
                Email = "elena.rostova@example.com",
                Department = "Software Engineering",
                CreatedAt = DateTimeOffset.UtcNow.AddMonths(-3)
            };

            var marcus = new Employee
            {
                Id = MarcusVanceId,
                FullName = "Marcus Vance",
                Email = "marcus.vance@example.com",
                Department = "Engineering Management",
                CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6)
            };

            await _dbContext.Employees.AddRangeAsync(new[] { elena, marcus }, ct);
            await _dbContext.SaveChangesAsync(ct);

            var seedGoals = new[]
            {
                new Goal
                {
                    Id = Guid.Parse("f0a1b2c3-d4e5-0000-0000-000000000001"),
                    EmployeeId = ElenaRostovaId,
                    Title = "Migrate legacy auth to OAuth 2.1",
                    Description = "Upgrade internal identity provider services to adhere to OAuth 2.1 RFC specifications and deprecate legacy password grants.",
                    Category = GoalCategory.TECHNICAL,
                    Metric = "Zero auth downtime, 100% services migrated to OAuth 2.1",
                    Timeframe = "Q3 2026",
                    Priority = GoalPriority.HIGH,
                    Status = GoalStatus.ACTIVE,
                    Provenance = GoalProvenance.MANUAL,
                    SourceTranscriptHash = "0000000000000000000000000000000000000000000000000000000000000001",
                    ReviewerId = MarcusVanceId,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-14)
                },
                new Goal
                {
                    Id = Guid.Parse("f0a1b2c3-d4e5-0000-0000-000000000002"),
                    EmployeeId = ElenaRostovaId,
                    Title = "Improve unit test coverage to 85%",
                    Description = "Increase test coverage across domain and application core business logic to at least 85%.",
                    Category = GoalCategory.DEVELOPMENT,
                    Metric = "Unit test code coverage >= 85% in CI pipeline",
                    Timeframe = "Q4 2026",
                    Priority = GoalPriority.MEDIUM,
                    Status = GoalStatus.ACTIVE,
                    Provenance = GoalProvenance.MANUAL,
                    SourceTranscriptHash = "0000000000000000000000000000000000000000000000000000000000000002",
                    ReviewerId = MarcusVanceId,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-7)
                }
            };

            await _dbContext.Goals.AddRangeAsync(seedGoals, ct);
            await _dbContext.SaveChangesAsync(ct);

            _logger?.LogInformation("Database seeded successfully with {Count} employees and {GoalCount} goals.", 2, seedGoals.Length);
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(ct);
    }
}
