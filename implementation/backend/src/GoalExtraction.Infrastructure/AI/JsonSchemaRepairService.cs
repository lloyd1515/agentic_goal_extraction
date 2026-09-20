namespace GoalExtraction.Infrastructure.AI;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Domain.Enums;

public class JsonSchemaValidationException : Exception
{
    public JsonSchemaValidationException(string message) : base(message) { }
    public JsonSchemaValidationException(string message, Exception innerException) : base(message, innerException) { }
}

public class JsonSchemaRepairService
{
    private static readonly Regex EmbeddedFenceRegex = new(
        @"```(?:json)?\s*([\s\S]*?)\s*```",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string StripMarkdownFences(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var text = input.Trim();

        var fenceMatch = EmbeddedFenceRegex.Match(text);
        if (fenceMatch.Success)
        {
            text = fenceMatch.Groups[1].Value.Trim();
        }
        else
        {
            // Fallback line-by-line fence strip
            if (text.StartsWith("```"))
            {
                var firstNewline = text.IndexOf('\n');
                text = firstNewline != -1 ? text.Substring(firstNewline + 1) : text.TrimStart('`');
            }

            if (text.EndsWith("```"))
            {
                var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence != -1)
                {
                    text = text.Substring(0, lastFence);
                }
            }

            text = text.Trim();
        }

        // If there's commentary text before or after, extract the outer JSON object
        var firstBrace = text.IndexOf('{');
        var lastBrace = text.LastIndexOf('}');
        if (firstBrace != -1 && lastBrace != -1 && lastBrace >= firstBrace)
        {
            text = text.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return text.Trim();
    }

    public ExtractedGoalsResult Parse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new JsonSchemaValidationException("LLM returned null or empty response.");
        }

        var cleaned = StripMarkdownFences(rawJson);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            throw new JsonSchemaValidationException("Stripped JSON content is empty.");
        }

        RawGoalExtractionPayload? payload;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            payload = JsonSerializer.Deserialize<RawGoalExtractionPayload>(cleaned, options);
        }
        catch (JsonException ex)
        {
            throw new JsonSchemaValidationException($"Malformed JSON syntax: {ex.Message}", ex);
        }

        if (payload == null)
        {
            throw new JsonSchemaValidationException("Deserialized JSON payload is null.");
        }

        if (string.IsNullOrWhiteSpace(payload.Summary))
        {
            throw new JsonSchemaValidationException("Missing required JSON property 'summary'.");
        }

        if (payload.Goals == null)
        {
            throw new JsonSchemaValidationException("Missing required JSON property 'goals'.");
        }

        var items = new List<ExtractedGoalItem>();
        for (int i = 0; i < payload.Goals.Count; i++)
        {
            var goal = payload.Goals[i];
            var goalIndex = i + 1;

            if (string.IsNullOrWhiteSpace(goal.Title))
            {
                throw new JsonSchemaValidationException($"Goal #{goalIndex} must contain a non-empty 'title'.");
            }

            var trimmedTitle = goal.Title.Trim();
            if (trimmedTitle.Length < 5 || trimmedTitle.Length > 120)
            {
                throw new JsonSchemaValidationException(
                    $"Goal #{goalIndex} title length ({trimmedTitle.Length}) must be between 5 and 120 characters.");
            }

            if (string.IsNullOrWhiteSpace(goal.Description))
            {
                throw new JsonSchemaValidationException($"Goal #{goalIndex} must contain a non-empty 'description'.");
            }

            var trimmedDescription = goal.Description.Trim();
            if (trimmedDescription.Length < 10 || trimmedDescription.Length > 1000)
            {
                throw new JsonSchemaValidationException(
                    $"Goal #{goalIndex} description length ({trimmedDescription.Length}) must be between 10 and 1000 characters.");
            }

            if (string.IsNullOrWhiteSpace(goal.Category))
            {
                throw new JsonSchemaValidationException($"Goal #{goalIndex} must contain a required 'category'.");
            }

            var category = goal.Category.Trim().ToUpperInvariant();
            if (!Enum.TryParse<GoalCategory>(category, true, out _))
            {
                throw new JsonSchemaValidationException(
                    $"Goal #{goalIndex} category '{category}' is invalid. Allowed: PERFORMANCE, DEVELOPMENT, PROJECT, TECHNICAL.");
            }

            if (string.IsNullOrWhiteSpace(goal.Metric))
            {
                throw new JsonSchemaValidationException($"Goal #{goalIndex} must contain a required 'metric'.");
            }

            if (string.IsNullOrWhiteSpace(goal.Timeframe))
            {
                throw new JsonSchemaValidationException($"Goal #{goalIndex} must contain a required 'timeframe'.");
            }

            if (string.IsNullOrWhiteSpace(goal.Priority))
            {
                throw new JsonSchemaValidationException($"Goal #{goalIndex} must contain a required 'priority'.");
            }

            var priority = goal.Priority.Trim().ToUpperInvariant();
            if (!Enum.TryParse<GoalPriority>(priority, true, out _))
            {
                throw new JsonSchemaValidationException(
                    $"Goal #{goalIndex} priority '{priority}' is invalid. Allowed: HIGH, MEDIUM, LOW.");
            }

            items.Add(new ExtractedGoalItem
            {
                Title = trimmedTitle,
                Description = trimmedDescription,
                Category = category,
                Metric = goal.Metric.Trim(),
                Timeframe = goal.Timeframe.Trim(),
                Priority = priority
            });
        }

        return new ExtractedGoalsResult
        {
            Summary = payload.Summary.Trim(),
            Goals = items,
            Success = true
        };
    }

    private sealed class RawGoalExtractionPayload
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("goals")]
        public List<RawGoalItemPayload>? Goals { get; set; }
    }

    private sealed class RawGoalItemPayload
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("metric")]
        public string? Metric { get; set; }

        [JsonPropertyName("timeframe")]
        public string? Timeframe { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }
    }
}
