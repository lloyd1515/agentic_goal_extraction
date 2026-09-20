namespace GoalExtraction.Application.AI.Models;

using System.Text.Json.Serialization;

public class ExtractedGoalItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "PERFORMANCE";

    [JsonPropertyName("metric")]
    public string Metric { get; set; } = string.Empty;

    [JsonPropertyName("timeframe")]
    public string Timeframe { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "MEDIUM";
}
