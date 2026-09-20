namespace GoalExtraction.Application.DTOs;

public class SaveGoalsBatchRequestDto
{
    public Guid EmployeeId { get; set; }
    public string? SourceTranscriptHash { get; set; }
    public Guid? ReviewerId { get; set; }
    public List<SaveGoalItemDto> Goals { get; set; } = new();
}
