namespace GoalExtraction.Application.Commands.SaveGoalsBatch;

using GoalExtraction.Application.Common;
using GoalExtraction.Application.DTOs;
using GoalExtraction.Domain.Entities;
using GoalExtraction.Domain.Enums;
using GoalExtraction.Domain.Interfaces;
using MediatR;

public class SaveGoalsBatchCommandHandler : IRequestHandler<SaveGoalsBatchCommand, SaveGoalsBatchResultDto>
{
    private readonly IGoalBatchWriteRepository _writeRepository;
    private readonly IReadOnlyGoalQueryRepository _queryRepository;

    public SaveGoalsBatchCommandHandler(
        IGoalBatchWriteRepository writeRepository,
        IReadOnlyGoalQueryRepository queryRepository)
    {
        _writeRepository = writeRepository ?? throw new ArgumentNullException(nameof(writeRepository));
        _queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
    }

    public async Task<SaveGoalsBatchResultDto> Handle(
        SaveGoalsBatchCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Validate employee existence via read-only query repository
        var employee = await _queryRepository.GetEmployeeByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new NotFoundException($"Employee with ID '{request.EmployeeId}' was not found.");
        }

        // 2. Map SaveGoalItemDto items to Domain Goal entities with GoalStatus.ACTIVE
        var goalEntities = (request.Goals ?? new List<SaveGoalItemDto>()).Select(item =>
        {
            var category = Enum.TryParse<GoalCategory>(item.Category, true, out var cat)
                ? cat
                : GoalCategory.PERFORMANCE;

            var priority = Enum.TryParse<GoalPriority>(item.Priority, true, out var prio)
                ? prio
                : GoalPriority.MEDIUM;

            var provenance = Enum.TryParse<GoalProvenance>(item.Provenance, true, out var prov)
                ? prov
                : GoalProvenance.AI_ORIGINAL;

            return new Goal
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                Title = item.Title.Trim(),
                Description = item.Description.Trim(),
                Category = category,
                Metric = item.Metric?.Trim() ?? string.Empty,
                Timeframe = item.Timeframe?.Trim() ?? string.Empty,
                Priority = priority,
                Status = GoalStatus.ACTIVE,
                Provenance = provenance,
                SourceTranscriptHash = request.SourceTranscriptHash?.Trim() ?? string.Empty,
                ReviewerId = request.ReviewerId,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }).ToList();

        // 3. Strict Invariant: Zero AI/LLM calls. Direct batch write persistence via repository.
        var savedGoalIds = await _writeRepository.SaveGoalsBatchAsync(goalEntities, cancellationToken);

        return new SaveGoalsBatchResultDto
        {
            CorrelationId = request.CorrelationId,
            Success = true,
            SavedCount = savedGoalIds.Count,
            SavedGoalIds = savedGoalIds.ToList(),
            SavedAt = DateTimeOffset.UtcNow
        };
    }
}
