namespace GoalExtraction.Api.Controllers;

using GoalExtraction.Api.Middleware;
using GoalExtraction.Application.Commands.ExtractGoals;
using GoalExtraction.Application.Commands.SaveGoalsBatch;
using GoalExtraction.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/goals")]
[Produces("application/json")]
public class GoalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GoalsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Extracts structured goals from a 1:1 conversation transcript using AI and existing employee context.
    /// </summary>
    /// <param name="request">The extraction request payload containing the transcript and optional employee ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted goals proposal with audit metadata.</returns>
    [HttpPost("extract")]
    [ProducesResponseType(typeof(GoalExtractionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GoalExtractionResultDto>> ExtractGoals(
        [FromBody] ExtractGoalsRequestDto request, 
        CancellationToken cancellationToken)
    {
        var correlationId = GetCorrelationId();
        var command = new ExtractGoalsCommand(
            request.Transcript,
            request.EmployeeId,
            request.Department,
            correlationId);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

        var result = await _mediator.Send(command, timeoutCts.Token);
        return Ok(result);
    }

    /// <summary>
    /// Atomically persists an approved batch of goals with audit history.
    /// </summary>
    /// <param name="request">The batch of goals to save with reviewer and audit information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Saved batch confirmation with created goal IDs.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(SaveGoalsBatchResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SaveGoalsBatchResultDto>> SaveGoalsBatch(
        [FromBody] SaveGoalsBatchRequestDto request, 
        CancellationToken cancellationToken)
    {
        var correlationId = GetCorrelationId();
        var command = new SaveGoalsBatchCommand(
            request.EmployeeId,
            request.SourceTranscriptHash,
            request.ReviewerId,
            request.Goals,
            correlationId);

        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    private string GetCorrelationId()
    {
        if (HttpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var item) &&
            item is string correlationId &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        if (HttpContext.Request.Headers.TryGetValue(CorrelationIdMiddleware.CorrelationIdHeaderName, out var headerVal) &&
            !string.IsNullOrWhiteSpace(headerVal))
        {
            return headerVal.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
