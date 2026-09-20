namespace GoalExtraction.Application.Commands.SaveGoalsBatch;

using GoalExtraction.Application.DTOs;
using MediatR;

public record SaveGoalsBatchCommand(
    Guid EmployeeId,
    string? SourceTranscriptHash,
    Guid? ReviewerId,
    List<SaveGoalItemDto> Goals,
    string CorrelationId = "") : IRequest<SaveGoalsBatchResultDto>;
