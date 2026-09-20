namespace GoalExtraction.Application.DTOs;

public class GoalExtractionResultDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public string TranscriptHash { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<ProposedGoalDto> Goals { get; set; } = new();
    public List<string> ToolCallsExecuted { get; set; } = new();
    public DateTimeOffset ExtractedAt { get; set; } = DateTimeOffset.UtcNow;
}
