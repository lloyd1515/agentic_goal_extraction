namespace GoalExtraction.Application.DTOs;

using System.Text.Json.Serialization;

public class ExtractGoalsContextDto
{
    [JsonPropertyName("employeeId")]
    public Guid? EmployeeId { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }
}

public class ContextDto : ExtractGoalsContextDto
{
}

public class ExtractGoalsRequestDto
{
    private Guid? _employeeId;
    private string? _department;

    [JsonPropertyName("transcript")]
    public string Transcript { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public ExtractGoalsContextDto? Context { get; set; }

    [JsonPropertyName("employeeId")]
    public Guid? EmployeeId
    {
        get => _employeeId ?? Context?.EmployeeId;
        set
        {
            _employeeId = value;
            if (Context != null && value.HasValue)
            {
                Context.EmployeeId = value;
            }
        }
    }

    [JsonPropertyName("department")]
    public string? Department
    {
        get => !string.IsNullOrWhiteSpace(_department) ? _department : Context?.Department;
        set
        {
            _department = value;
            if (Context != null && !string.IsNullOrWhiteSpace(value))
            {
                Context.Department = value;
            }
        }
    }
}
