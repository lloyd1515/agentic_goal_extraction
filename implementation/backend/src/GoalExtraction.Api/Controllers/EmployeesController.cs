namespace GoalExtraction.Api.Controllers;

using GoalExtraction.Application.Common;
using GoalExtraction.Application.DTOs;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/employees")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IReadOnlyGoalQueryRepository _queryRepository;

    public EmployeesController(IReadOnlyGoalQueryRepository queryRepository)
    {
        _queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
    }

    /// <summary>
    /// Returns the list of all seeded employees for UI selection and context switching.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of employees with id, full name, email, and department.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetEmployees(CancellationToken cancellationToken)
    {
        var employees = await _queryRepository.GetAllEmployeesAsync(cancellationToken);
        var result = employees.Select(e => new EmployeeDto
        {
            Id = e.Id,
            FullName = e.FullName,
            Email = e.Email,
            Department = e.Department
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Returns active existing goals for the specified employee to display historical context.
    /// </summary>
    /// <param name="id">The unique identifier of the employee.</param>
    /// <param name="limit">Maximum number of active goals to return (default 20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active goals for the employee.</returns>
    [HttpGet("{id:guid}/goals")]
    [ProducesResponseType(typeof(IReadOnlyList<Goal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<Goal>>> GetEmployeeGoals(
        Guid id, 
        [FromQuery] int limit = 20, 
        CancellationToken cancellationToken = default)
    {
        var employee = await _queryRepository.GetEmployeeByIdAsync(id, cancellationToken);
        if (employee == null)
        {
            throw new NotFoundException("Employee", id);
        }

        var goals = await _queryRepository.GetGoalsByEmployeeIdAsync(id, GoalStatus.ACTIVE, limit, cancellationToken);
        return Ok(goals);
    }
}
