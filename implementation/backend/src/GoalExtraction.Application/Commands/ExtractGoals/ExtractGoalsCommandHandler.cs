namespace GoalExtraction.Application.Commands.ExtractGoals;

using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Application.Common;
using GoalExtraction.Application.DTOs;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using MediatR;

public class ExtractGoalsCommandHandler : IRequestHandler<ExtractGoalsCommand, GoalExtractionResultDto>
{
    private readonly IGoalExtractionAgent _agent;
    private readonly IReadOnlyGoalQueryRepository _queryRepository;

    public ExtractGoalsCommandHandler(
        IGoalExtractionAgent agent,
        IReadOnlyGoalQueryRepository queryRepository)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
    }

    public async Task<GoalExtractionResultDto> Handle(
        ExtractGoalsCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Sanitize transcript and compute deterministic audit hash
        var sanitizedTranscript = TextSanitizer.SanitizeTranscript(request.Transcript);
        var transcriptHash = TextSanitizer.ComputeSha256Hash(sanitizedTranscript);

        // 2. Look up employee name if EmployeeId is provided (Read-only query)
        string? employeeName = null;
        if (request.EmployeeId.HasValue && request.EmployeeId.Value != Guid.Empty)
        {
            var employee = await _queryRepository.GetEmployeeByIdAsync(request.EmployeeId.Value, cancellationToken);
            employeeName = employee?.FullName;
        }

        // 3. Prepare AI Agent context
        var context = new AgentExtractionContext(
            transcript: sanitizedTranscript,
            employeeId: request.EmployeeId,
            employeeName: employeeName,
            reviewerId: null);

        // 4. Strict Invariant: Invoke read-only AI agent (ZERO database write operations)
        var agentResult = await _agent.ExtractGoalsAsync(context, cancellationToken);

        if (!agentResult.Success)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(agentResult.ErrorMessage)
                    ? "AI goal extraction failed to produce valid results."
                    : agentResult.ErrorMessage);
        }

        // 5. Map result into GoalExtractionResultDto with unique TempId for each candidate goal
        var proposedGoals = (agentResult.Goals ?? new List<ExtractedGoalItem>()).Select(g => new ProposedGoalDto
        {
            TempId = Guid.NewGuid(),
            Title = g.Title ?? string.Empty,
            Description = g.Description ?? string.Empty,
            Category = string.IsNullOrWhiteSpace(g.Category) ? "PERFORMANCE" : g.Category.Trim().ToUpperInvariant(),
            Metric = g.Metric ?? string.Empty,
            Timeframe = g.Timeframe ?? string.Empty,
            Priority = string.IsNullOrWhiteSpace(g.Priority) ? "MEDIUM" : g.Priority.Trim().ToUpperInvariant(),
            Provenance = GoalProvenance.AI_ORIGINAL.ToString()
        }).ToList();

        return new GoalExtractionResultDto
        {
            CorrelationId = request.CorrelationId,
            TranscriptHash = transcriptHash,
            Summary = agentResult.Summary ?? string.Empty,
            Goals = proposedGoals,
            ToolCallsExecuted = agentResult.ToolCallsExecuted ?? new List<string>(),
            ExtractedAt = DateTimeOffset.UtcNow
        };
    }
}
