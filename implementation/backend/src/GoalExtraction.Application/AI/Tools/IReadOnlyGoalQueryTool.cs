namespace GoalExtraction.Application.AI.Tools;

public interface IReadOnlyGoalQueryTool
{
    string ToolName => "query_existing_employee_goals";

    object GetToolDeclaration();

    Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default);
}
