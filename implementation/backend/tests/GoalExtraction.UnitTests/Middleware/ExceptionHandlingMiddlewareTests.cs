namespace GoalExtraction.UnitTests.Middleware;

using System.IO;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using GoalExtraction.Api.Middleware;
using GoalExtraction.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class ExceptionHandlingMiddlewareTests
{
    private readonly NullLogger<ExceptionHandlingMiddleware> _logger = NullLogger<ExceptionHandlingMiddleware>.Instance;
    private readonly Mock<IHostEnvironment> _envMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _envMock = new Mock<IHostEnvironment>();
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
    }

    private DefaultHttpContext CreateContext(string correlationId = "test-corr-id-123")
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/goals/extract";
        context.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
        return context;
    }

    private static async Task<ProblemDetails> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ProblemDetails>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    [Fact]
    public async Task InvokeAsync_OnValidationException_Returns400WithErrorsDictionary()
    {
        // Arrange
        var context = CreateContext();
        var failures = new List<ValidationFailure>
        {
            new("Transcript", "Transcript must be at least 20 characters."),
            new("Transcript", "Transcript cannot be empty.")
        };
        var middleware = new ExceptionHandlingMiddleware(_ => throw new ValidationException(failures), _logger, _envMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Contain("application/problem+json");

        var problem = await ReadProblemDetailsAsync(context);
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Title.Should().Be("Validation Error");
        problem.Extensions.Should().ContainKey("errors");
        problem.Extensions.Should().ContainKey("correlationId");
        problem.Extensions["correlationId"]?.ToString().Should().Be("test-corr-id-123");

        var errorsJson = JsonSerializer.Serialize(problem.Extensions["errors"]);
        errorsJson.Should().Contain("Transcript must be at least 20 characters.");
    }

    [Fact]
    public async Task InvokeAsync_OnNotFoundException_Returns404NotFound()
    {
        // Arrange
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new NotFoundException("Employee", Guid.NewGuid()), _logger, _envMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = await ReadProblemDetailsAsync(context);
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Title.Should().Be("Not Found");
        problem.Detail.Should().Contain("was not found");
    }

    [Fact]
    public async Task InvokeAsync_OnKeyNotFoundException_Returns404NotFound()
    {
        // Arrange
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new KeyNotFoundException("Item was not found."), _logger, _envMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = await ReadProblemDetailsAsync(context);
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Title.Should().Be("Not Found");
    }

    [Fact]
    public async Task InvokeAsync_OnTimeoutException_Returns504GatewayTimeout()
    {
        // Arrange
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new TimeoutException("LLM request timed out."), _logger, _envMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status504GatewayTimeout);
        var problem = await ReadProblemDetailsAsync(context);
        problem.Status.Should().Be(StatusCodes.Status504GatewayTimeout);
        problem.Title.Should().Be("Gateway Timeout");
        problem.Detail.Should().Be("LLM request timed out.");
    }

    [Fact]
    public async Task InvokeAsync_OnTaskCanceledException_Returns504GatewayTimeout()
    {
        // Arrange
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new TaskCanceledException(), _logger, _envMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status504GatewayTimeout);
        var problem = await ReadProblemDetailsAsync(context);
        problem.Status.Should().Be(StatusCodes.Status504GatewayTimeout);
        problem.Title.Should().Be("Gateway Timeout");
    }

    [Fact]
    public async Task InvokeAsync_OnArgumentException_Returns400BadRequest()
    {
        // Arrange
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new ArgumentException("Invalid argument provided."), _logger, _envMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problem = await ReadProblemDetailsAsync(context);
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Title.Should().Be("Bad Request");
        problem.Detail.Should().Be("Invalid argument provided.");
    }

    [Fact]
    public async Task InvokeAsync_OnInvalidOperationException_Returns422UnprocessableEntity()
    {
        // Arrange
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("Invalid business state."), _logger, _envMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = await ReadProblemDetailsAsync(context);
        problem.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problem.Title.Should().Be("Unprocessable Entity");
    }

    [Fact]
    public async Task InvokeAsync_OnGeneralException_Returns500InternalServerError()
    {
        // Arrange
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new Exception("Unexpected catastrophic failure!"), _logger, _envMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var problem = await ReadProblemDetailsAsync(context);
        problem.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Title.Should().Be("Internal Server Error");
    }
}
