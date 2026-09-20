namespace GoalExtraction.Infrastructure.Persistence;

using GoalExtraction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class GoalExtractionDbContext : DbContext
{
    protected GoalExtractionDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public GoalExtractionDbContext(DbContextOptions<GoalExtractionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Goal> Goals => Set<Goal>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<long>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Department).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasMany(e => e.Goals)
                  .WithOne(g => g.Employee)
                  .HasForeignKey(g => g.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.ToTable("Goals");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Title).IsRequired().HasMaxLength(120);
            entity.Property(g => g.Description).IsRequired().HasMaxLength(1000);
            entity.Property(g => g.Category).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(g => g.Metric).IsRequired().HasMaxLength(500);
            entity.Property(g => g.Timeframe).IsRequired().HasMaxLength(100);
            entity.Property(g => g.Priority).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(g => g.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(g => g.Provenance).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(g => g.SourceTranscriptHash).IsRequired().HasMaxLength(64);
            entity.Property(g => g.ReviewerId);
            entity.Property(g => g.CreatedAt).IsRequired();

            entity.HasIndex(g => g.EmployeeId);
            entity.HasIndex(g => g.Status);
            entity.HasIndex(g => new { g.EmployeeId, g.Status });
        });
    }
}
