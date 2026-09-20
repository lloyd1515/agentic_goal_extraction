namespace GoalExtraction.Infrastructure.Persistence.Repositories;

using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using GoalExtraction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
public class ReadOnlyGoalQueryRepository : IReadOnlyGoalQueryRepository
{
    private readonly ReadOnlyGoalExtractionDbContext _dbContext;

    public ReadOnlyGoalQueryRepository(ReadOnlyGoalExtractionDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<Goal>> GetGoalsByEmployeeIdAsync(
        Guid employeeId, 
        GoalStatus? status = null, 
        int limit = 10, 
        CancellationToken ct = default)
    {
        var query = _dbContext.Goals
            .AsNoTracking()
            .Where(g => g.EmployeeId == employeeId);

        if (status.HasValue)
        {
            query = query.Where(g => g.Status == status.Value);
        }

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Goal>> SearchGoalsAsync(
        string employeeNameOrQuery, 
        int limit = 5, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(employeeNameOrQuery))
        {
            return Array.Empty<Goal>();
        }

        var term = employeeNameOrQuery.Trim();

        return await _dbContext.Goals
            .AsNoTracking()
            .Include(g => g.Employee)
            .Where(g => (g.Employee != null && EF.Functions.Like(g.Employee.FullName, $"%{term}%"))
                     || EF.Functions.Like(g.Title, $"%{term}%")
                     || EF.Functions.Like(g.Description, $"%{term}%"))
            .OrderByDescending(g => g.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<Employee?> GetEmployeeByIdAsync(
        Guid employeeId, 
        CancellationToken ct = default)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
    }

    public async Task<Employee?> GetEmployeeByNameAsync(
        string name, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();

        var exactMatch = await _dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Functions.Like(e.FullName, trimmed), ct);

        if (exactMatch != null)
        {
            return exactMatch;
        }

        return await _dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Functions.Like(e.FullName, $"%{trimmed}%"), ct);
    }

    public async Task<IReadOnlyList<Employee>> GetAllEmployeesAsync(
        CancellationToken ct = default)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .OrderBy(e => e.FullName)
            .ToListAsync(ct);
    }
}
