namespace GoalExtraction.Domain.Interfaces;

using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;

public interface IReadOnlyGoalQueryRepository
{
    Task<IReadOnlyList<Goal>> GetGoalsByEmployeeIdAsync(
        Guid employeeId, 
        GoalStatus? status = null, 
        int limit = 10, 
        CancellationToken ct = default);

    Task<IReadOnlyList<Goal>> SearchGoalsAsync(
        string employeeNameOrQuery, 
        int limit = 5, 
        CancellationToken ct = default);

    Task<Employee?> GetEmployeeByIdAsync(
        Guid employeeId, 
        CancellationToken ct = default);

    Task<Employee?> GetEmployeeByNameAsync(
        string name, 
        CancellationToken ct = default);

    Task<IReadOnlyList<Employee>> GetAllEmployeesAsync(
        CancellationToken ct = default);
}
