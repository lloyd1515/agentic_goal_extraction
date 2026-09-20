namespace GoalExtraction.UnitTests.Middleware;

using FluentAssertions;
using GoalExtraction.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class CorrelationIdMiddlewareTests
{
    private readonly NullLogger<CorrelationIdMiddleware> _logger = NullLogger<CorrelationIdMiddleware>.Instance;

    [Fact]
    public async Task InvokeAsync_WhenHeaderPresent_PreservesExistingCorrelationId()
    {
        // Arrange
        const string existingCorrelationId = "custom-test-correlation-id-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName] = existingCorrelationId;

        string? capturedItemId = null;
        var middleware = new CorrelationIdMiddleware(nextContext =>
        {
            capturedItemId = nextContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
            return Task.CompletedTask;
        }, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedItemId.Should().Be(existingCorrelationId);
        context.Items[CorrelationIdMiddleware.CorrelationIdItemKey].Should().Be(existingCorrelationId);
        context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].ToString().Should().Be(existingCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderMissing_GeneratesNewGuidCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();

        string? capturedItemId = null;
        var middleware = new CorrelationIdMiddleware(nextContext =>
        {
            capturedItemId = nextContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
            return Task.CompletedTask;
        }, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedItemId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(capturedItemId, out var parsedGuid).Should().BeTrue("Correlation ID should be a valid GUID");
        context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].ToString().Should().Be(capturedItemId);
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderWhitespace_GeneratesNewGuidCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName] = "   ";

        string? capturedItemId = null;
        var middleware = new CorrelationIdMiddleware(nextContext =>
        {
            capturedItemId = nextContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
            return Task.CompletedTask;
        }, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedItemId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(capturedItemId, out _).Should().BeTrue();
        context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].ToString().Should().Be(capturedItemId);
    }
}
