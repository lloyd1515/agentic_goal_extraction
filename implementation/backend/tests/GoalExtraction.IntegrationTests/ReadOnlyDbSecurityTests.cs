namespace GoalExtraction.IntegrationTests;

using FluentAssertions;
using GoalExtraction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ReadOnlyDbSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReadOnlyDbSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public void AgentConnection_WriteAttempt_ThrowsSecurityException()
    {
        using var scope = _factory.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        using var readOnlyConn = factory.CreateReadOnlyConnection();

        var act = () =>
        {
            using var cmd = readOnlyConn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Goals (Id, EmployeeId, Title, Description, Category, Metric, Timeframe, Priority, Status, Provenance, SourceTranscriptHash, CreatedAt)
                VALUES ('11111111-1111-1111-1111-111111111111', 'e0a1b2c3-d4e5-0000-0000-000000000001', 'Hacked Goal', 'Malicious write', 'TECHNICAL', 'N/A', 'ASAP', 'HIGH', 'ACTIVE', 'MANUAL', 'abc', '2026-09-19T00:00:00Z');";
            cmd.ExecuteNonQuery();
        };

        var exception = act.Should().Throw<SqliteException>().Which;
        exception.SqliteErrorCode.Should().Be(8, "SQLite error code 8 corresponds to SQLITE_READONLY");
        exception.Message.ToLower().Should().Contain("readonly");
    }

    [Fact]
    public void AgentConnection_UpdateAttempt_ThrowsSecurityException()
    {
        using var scope = _factory.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        using var readOnlyConn = factory.CreateReadOnlyConnection();

        var act = () =>
        {
            using var cmd = readOnlyConn.CreateCommand();
            cmd.CommandText = "UPDATE Goals SET Title = 'Unauthorized Update' WHERE 1=1;";
            cmd.ExecuteNonQuery();
        };

        var exception = act.Should().Throw<SqliteException>().Which;
        exception.SqliteErrorCode.Should().Be(8, "SQLite error code 8 corresponds to SQLITE_READONLY");
        exception.Message.ToLower().Should().Contain("readonly");
    }
}
