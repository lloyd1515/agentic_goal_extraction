namespace GoalExtraction.Application.AI;

using GoalExtraction.Application.AI.Models;

public interface IGoalExtractionAgent
{
    Task<ExtractedGoalsResult> ExtractGoalsAsync(
        AgentExtractionContext context, 
        CancellationToken ct = default);
}
