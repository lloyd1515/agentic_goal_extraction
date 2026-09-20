namespace GoalExtraction.Application.DTOs;

public class SaveGoalItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "PERFORMANCE";
    public string Metric { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public string Priority { get; set; } = "MEDIUM";
    public string Provenance { get; set; } = "AI_ORIGINAL";
}
