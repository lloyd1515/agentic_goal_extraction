namespace GoalExtraction.Infrastructure.AI;

using System.Text;
using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Application.AI.Tools;
using GoalExtraction.Infrastructure.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class GoalExtractionAgent : IGoalExtractionAgent
{
    private readonly IGeminiOpenAiClient _geminiClient;
    private readonly IReadOnlyGoalQueryTool _queryTool;
    private readonly JsonSchemaRepairService _jsonRepairService;
    private readonly GeminiAiOptions _options;
    private readonly ILogger<GoalExtractionAgent> _logger;

    public const string DefaultSystemPrompt = 
"""
You are a Senior Engineering & People Operations Performance Lead specializing in 1-on-1 performance reviews, engineering goal setting, and career development.

Your objective:
Carefully analyze the transcript of the 1-on-1 or performance review discussion between a manager/lead and an employee. Extract concrete, actionable, and measurable SMART (Specific, Measurable, Achievable, Relevant, Time-bound) goals mutually agreed upon or assigned during the meeting.

Instructions:
1. Context Tool Calling:
   - If an employee ID or employee name is provided in the context, CALL the tool `query_existing_employee_goals` to inspect existing active goals.
   - Use the existing goals to avoid creating exact duplicates and to synthesize updated progress, increased metrics, or next milestones discussed in the conversation.
2. Goal Criteria:
   - Extract only actionable commitments, milestones, technical deliverables, or professional development goals agreed upon.
   - Do NOT invent commitments not discussed in the transcript.
   - If the transcript is casual conversation, small talk, or social banter without actionable performance or development goals, return an empty goals list: `[]` with an informative summary explaining that no actionable commitments were discussed.
3. Output Format:
   - Respond ONLY with valid, raw JSON. Do NOT include markdown formatting or code blocks (```json).
   - Format:
   {
     "summary": "High-level summary of the meeting and alignment",
     "goals": [
       {
         "title": "Concise SMART title (5-120 characters)",
         "description": "Detailed explanation of expectations and outcomes (10-1000 characters)",
         "category": "PERFORMANCE | DEVELOPMENT | PROJECT | TECHNICAL",
         "metric": "Key measurable indicator or verifiable definition of done",
         "timeframe": "Target timeframe or deadline (e.g. Q4 2026, by End of Sprint 24)",
         "priority": "HIGH | MEDIUM | LOW"
       }
     ]
   }
""";

    public GoalExtractionAgent(
        IGeminiOpenAiClient geminiClient,
        IReadOnlyGoalQueryTool queryTool,
        JsonSchemaRepairService jsonRepairService,
        IOptions<GeminiAiOptions> options,
        ILogger<GoalExtractionAgent> logger)
    {
        _geminiClient = geminiClient ?? throw new ArgumentNullException(nameof(geminiClient));
        _queryTool = queryTool ?? throw new ArgumentNullException(nameof(queryTool));
        _jsonRepairService = jsonRepairService ?? throw new ArgumentNullException(nameof(jsonRepairService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExtractedGoalsResult> ExtractGoalsAsync(
        AgentExtractionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var toolCallsExecuted = new List<string>();

        var userPromptBuilder = new StringBuilder();
        userPromptBuilder.AppendLine("Transcript of Review / 1-on-1 Meeting:");
        userPromptBuilder.AppendLine("\"\"\"");
        userPromptBuilder.AppendLine(context.Transcript);
        userPromptBuilder.AppendLine("\"\"\"");
        userPromptBuilder.AppendLine();
        userPromptBuilder.AppendLine("Context Information:");
        userPromptBuilder.AppendLine($"- Employee ID: {(context.EmployeeId.HasValue ? context.EmployeeId.Value.ToString() : "Not provided")}");
        userPromptBuilder.AppendLine($"- Employee Name: {(string.IsNullOrWhiteSpace(context.EmployeeName) ? "Not provided" : context.EmployeeName)}");
        userPromptBuilder.AppendLine($"- Reviewer ID: {(context.ReviewerId.HasValue ? context.ReviewerId.Value.ToString() : "Not provided")}");

        var messages = new List<ChatMessage>
        {
            ChatMessage.System(DefaultSystemPrompt),
            ChatMessage.User(userPromptBuilder.ToString())
        };

        var tools = new List<object> { _queryTool.GetToolDeclaration() };

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages = messages,
            Tools = tools,
            Temperature = 0.1
        };

        const int maxTurns = 3;
        int turn = 0;

        while (turn < maxTurns)
        {
            ChatCompletionResponse response;
            try
            {
                response = await _geminiClient.CreateChatCompletionAsync(request, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Goal extraction timed out or was cancelled by upstream caller.");
                throw new TimeoutException("Goal extraction timed out. Please retry.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute Gemini chat completion");
                return new ExtractedGoalsResult
                {
                    Success = false,
                    ErrorMessage = $"LLM request failed: {ex.Message}",
                    ToolCallsExecuted = toolCallsExecuted
                };
            }

            var choice = response.Choices.FirstOrDefault();
            if (choice == null)
            {
                return new ExtractedGoalsResult
                {
                    Success = false,
                    ErrorMessage = "LLM returned empty choices.",
                    ToolCallsExecuted = toolCallsExecuted
                };
            }

            var assistantMessage = choice.Message;

            // Check if LLM requested tool calls
            if (assistantMessage.ToolCalls != null && assistantMessage.ToolCalls.Count > 0)
            {
                messages.Add(assistantMessage);

                foreach (var toolCall in assistantMessage.ToolCalls)
                {
                    var toolName = toolCall.Function.Name;
                    toolCallsExecuted.Add(toolName);
                    _logger.LogInformation("Agent executing tool '{ToolName}' with arguments {Arguments}", 
                        toolName, toolCall.Function.Arguments);

                    string toolResult;
                    if (string.Equals(toolName, _queryTool.ToolName, StringComparison.OrdinalIgnoreCase))
                    {
                        toolResult = await _queryTool.ExecuteAsync(toolCall.Function.Arguments, ct);
                    }
                    else
                    {
                        toolResult = "{\"error\": \"Unrecognized tool requested.\"}";
                    }

                    messages.Add(ChatMessage.Tool(toolCall.Id, toolResult));
                }

                turn++;
                request = new ChatCompletionRequest
                {
                    Model = _options.Model,
                    Messages = messages,
                    Tools = tools,
                    Temperature = 0.1
                };
                continue;
            }

            // Final response returned without tool calls
            var rawContent = assistantMessage.Content ?? string.Empty;

            try
            {
                var result = _jsonRepairService.Parse(rawContent);
                result.ToolCallsExecuted = toolCallsExecuted;
                result.Success = true;
                return result;
            }
            catch (JsonSchemaValidationException ex)
            {
                _logger.LogWarning(ex, "Initial JSON response parsing failed. Initiating repair prompt...");

                // Attempt one-time repair retry
                messages.Add(ChatMessage.Assistant(rawContent));
                messages.Add(ChatMessage.User(
                    $"The previous response failed schema validation: {ex.Message}. " +
                    "Return ONLY valid raw JSON with 'summary' (string) and 'goals' (array). Do not use markdown code blocks."));

                var repairRequest = new ChatCompletionRequest
                {
                    Model = _options.Model,
                    Messages = messages,
                    Temperature = 0.1,
                    ResponseFormat = new ResponseFormat { Type = "json_object" }
                };

                try
                {
                    var repairResponse = await _geminiClient.CreateChatCompletionAsync(repairRequest, ct);
                    var repairContent = repairResponse.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
                    var repairedResult = _jsonRepairService.Parse(repairContent);
                    repairedResult.ToolCallsExecuted = toolCallsExecuted;
                    repairedResult.Success = true;
                    return repairedResult;
                }
                catch (Exception repairEx)
                {
                    _logger.LogError(repairEx, "Failed to parse JSON response even after repair attempt.");
                    return new ExtractedGoalsResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to parse structured goals: {repairEx.Message}",
                        ToolCallsExecuted = toolCallsExecuted
                    };
                }
            }
        }

        return new ExtractedGoalsResult
        {
            Success = false,
            ErrorMessage = "Exceeded maximum tool calling turns without final response.",
            ToolCallsExecuted = toolCallsExecuted
        };
    }
}
