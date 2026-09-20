namespace GoalExtraction.Application.AI.Models;

public class AgentExtractionContext
{
    public string Transcript { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public Guid? ReviewerId { get; set; }

    public AgentExtractionContext() { }

    public AgentExtractionContext(
        string transcript,
        Guid? employeeId = null,
        string? employeeName = null,
        Guid? reviewerId = null)
    {
        Transcript = transcript;
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        ReviewerId = reviewerId;
    }
}
