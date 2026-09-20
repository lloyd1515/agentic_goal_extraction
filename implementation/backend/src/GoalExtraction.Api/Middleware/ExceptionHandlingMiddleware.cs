namespace GoalExtraction.Api.Middleware;

using System.Text.Json;
using System.Text.Json.Serialization;
using GoalExtraction.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started, cannot write ProblemDetails for exception: {Message}", exception.Message);
            return;
        }

        string correlationId = GetCorrelationId(context);

        int statusCode;
        string title;
        string detail;
        Dictionary<string, string[]>? validationErrors = null;

        switch (exception)
        {
            case FluentValidation.ValidationException valEx:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Validation Error";
                detail = "One or more validation failures occurred.";
                validationErrors = valEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            case NotFoundException notFoundEx:
                statusCode = StatusCodes.Status404NotFound;
                title = "Not Found";
                detail = notFoundEx.Message;
                break;

            case KeyNotFoundException keyNotFoundEx:
                statusCode = StatusCodes.Status404NotFound;
                title = "Not Found";
                detail = keyNotFoundEx.Message;
                break;

            case TimeoutException timeoutEx:
                statusCode = StatusCodes.Status504GatewayTimeout;
                title = "Gateway Timeout";
                detail = !string.IsNullOrWhiteSpace(timeoutEx.Message)
                    ? timeoutEx.Message
                    : "Goal extraction timed out. Please retry.";
                break;

            case TaskCanceledException:
                statusCode = StatusCodes.Status504GatewayTimeout;
                title = "Gateway Timeout";
                detail = "Goal extraction timed out. Please retry.";
                break;

            case ArgumentException argEx:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Bad Request";
                detail = argEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                title = "Unprocessable Entity";
                detail = invalidOpEx.Message;
                break;

            case BadHttpRequestException badHttpEx:
                statusCode = badHttpEx.StatusCode;
                title = "Bad Request";
                detail = badHttpEx.Message;
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "Internal Server Error";
                detail = _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred while processing your request.";
                break;
        }

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled server error processing request {Path} [CorrelationId: {CorrelationId}]",
                context.Request.Path, correlationId);
        }
        else
        {
            _logger.LogWarning(exception, "Handled error processing request {Path} (Status {StatusCode}) [CorrelationId: {CorrelationId}]",
                context.Request.Path, statusCode, correlationId);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        if (validationErrors != null)
        {
            problemDetails.Extensions["errors"] = validationErrors;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        // Ensure correlation header is still on response
        if (!context.Response.Headers.ContainsKey(CorrelationIdMiddleware.CorrelationIdHeaderName))
        {
            context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName] = correlationId;
        }

        if (context.Request.Headers.TryGetValue("Origin", out var origin) &&
            !string.IsNullOrWhiteSpace(origin) &&
            !context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin.ToString();
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
        }

        await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, JsonOptions);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var val) &&
            val is string itemCorrelationId &&
            !string.IsNullOrWhiteSpace(itemCorrelationId))
        {
            return itemCorrelationId;
        }

        if (context.Request.Headers.TryGetValue(CorrelationIdMiddleware.CorrelationIdHeaderName, out var headerVal) &&
            !string.IsNullOrWhiteSpace(headerVal))
        {
            return headerVal.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
