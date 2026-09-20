namespace GoalExtraction.Application.AI;

using GoalExtraction.Application.AI.Models;

public interface IGeminiOpenAiClient
{
    Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request, 
        CancellationToken ct = default);
}
