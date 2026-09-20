namespace GoalExtraction.Application.Commands.ExtractGoals;

using GoalExtraction.Application.DTOs;
using MediatR;

public record ExtractGoalsCommand(
    string Transcript,
    Guid? EmployeeId = null,
    string? Department = null,
    string CorrelationId = "") : IRequest<GoalExtractionResultDto>;
