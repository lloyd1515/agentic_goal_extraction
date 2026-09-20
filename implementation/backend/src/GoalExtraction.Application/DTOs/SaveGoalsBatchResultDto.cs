namespace GoalExtraction.Application.DTOs;

using System.Text.Json.Serialization;

public class SaveGoalsBatchResultDto
{
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("savedCount")]
    public int SavedCount { get; set; }

    [JsonPropertyName("persistedCount")]
    public int PersistedCount => SavedCount;

    [JsonPropertyName("savedGoalIds")]
    public List<Guid> SavedGoalIds { get; set; } = new();

    [JsonPropertyName("goalIds")]
    public List<Guid> GoalIds => SavedGoalIds;

    [JsonPropertyName("savedAt")]
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;
}
