namespace GoalExtraction.Infrastructure.AI.Options;

public class GeminiAiOptions
{
    public const string SectionName = "GeminiAi";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/openai/";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-flash-lite-latest";

    public int TimeoutSeconds { get; set; } = 60;
}
