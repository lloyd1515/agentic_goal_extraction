namespace GoalExtraction.Application.AI.Tools;

using System.Text.Json;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;

public class ReadOnlyGoalQueryTool : IReadOnlyGoalQueryTool
{
    private readonly IReadOnlyGoalQueryRepository _repository;

    public string ToolName => "query_existing_employee_goals";

    public ReadOnlyGoalQueryTool(IReadOnlyGoalQueryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public object GetToolDeclaration()
    {
        return new
        {
            type = "function",
            function = new
            {
                name = ToolName,
                description = "Queries existing performance and development goals for an employee from the read-only database. Call this tool when employee context (ID or name) is available in the conversation to review prior goals and prevent duplicates.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        employee_id = new
                        {
                            type = "string",
                            description = "The UUID of the employee (e.g. 'e0a1b2c3-d4e5-0000-0000-000000000001')."
                        },
                        employee_name = new
                        {
                            type = "string",
                            description = "The full or partial name of the employee."
                        },
                        status = new
                        {
                            type = "string",
                            @enum = new[] { "ACTIVE", "COMPLETED", "ARCHIVED", "ALL" },
                            description = "Goal status filter. Defaults to ACTIVE."
                        },
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum number of goals to return (default 10)."
                        }
                    }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        try
        {
            Guid? employeeId = null;
            string? employeeName = null;
            GoalStatus? statusFilter = GoalStatus.ACTIVE;
            int limit = 10;

            if (!string.IsNullOrWhiteSpace(argumentsJson))
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("employee_id", out var idProp) || root.TryGetProperty("employeeId", out idProp))
                {
                    if (idProp.ValueKind == JsonValueKind.String && Guid.TryParse(idProp.GetString(), out var parsedId))
                    {
                        employeeId = parsedId;
                    }
                }

                if (root.TryGetProperty("employee_name", out var nameProp) || root.TryGetProperty("employeeName", out nameProp))
                {
                    if (nameProp.ValueKind == JsonValueKind.String)
                    {
                        employeeName = nameProp.GetString();
                    }
                }

                if (root.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.String)
                {
                    var statusStr = statusProp.GetString();
                    if (string.Equals(statusStr, "ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        statusFilter = null;
                    }
                    else if (Enum.TryParse<GoalStatus>(statusStr, true, out var parsedStatus))
                    {
                        statusFilter = parsedStatus;
                    }
                }

                if (root.TryGetProperty("limit", out var limitProp) && limitProp.ValueKind == JsonValueKind.Number)
                {
                    if (limitProp.TryGetInt32(out var parsedLimit) && parsedLimit > 0)
                    {
                        limit = Math.Min(parsedLimit, 50);
                    }
                }
            }

            Employee? employee = null;
            IReadOnlyList<Goal> goals = Array.Empty<Goal>();

            if (employeeId.HasValue)
            {
                employee = await _repository.GetEmployeeByIdAsync(employeeId.Value, ct);
                goals = await _repository.GetGoalsByEmployeeIdAsync(employeeId.Value, statusFilter, limit, ct);
            }
            else if (!string.IsNullOrWhiteSpace(employeeName))
            {
                employee = await _repository.GetEmployeeByNameAsync(employeeName, ct);
                if (employee != null)
                {
                    goals = await _repository.GetGoalsByEmployeeIdAsync(employee.Id, statusFilter, limit, ct);
                }
                else
                {
                    goals = await _repository.SearchGoalsAsync(employeeName, limit, ct);
                }
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    employee = (object?)null,
                    count = 0,
                    goals = Array.Empty<object>(),
                    message = "No employee ID or employee name specified in tool arguments."
                });
            }

            var payload = new
            {
                employee = employee != null ? new
                {
                    id = employee.Id,
                    name = employee.FullName,
                    department = employee.Department,
                    email = employee.Email
                } : null,
                count = goals.Count,
                goals = goals.Select(g => new
                {
                    id = g.Id,
                    title = g.Title,
                    description = g.Description,
                    category = g.Category.ToString(),
                    metric = g.Metric,
                    timeframe = g.Timeframe,
                    priority = g.Priority.ToString(),
                    status = g.Status.ToString(),
                    createdAt = g.CreatedAt
                }).ToList()
            };

            return JsonSerializer.Serialize(payload);
        }
        catch (Exception)
        {
            // Fault Tolerance: Never throws on DB errors; returns graceful fallback
            return "{\"error\": \"Context database unavailable\"}";
        }
    }
}
