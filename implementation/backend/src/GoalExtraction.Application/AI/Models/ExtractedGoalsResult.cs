namespace GoalExtraction.Application.AI.Models;

using System.Text.Json.Serialization;

public class ExtractedGoalsResult
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("goals")]
    public List<ExtractedGoalItem> Goals { get; set; } = new();

    public List<string> ToolCallsExecuted { get; set; } = new();

    public bool Success { get; set; } = true;

    public string? ErrorMessage { get; set; }
}
