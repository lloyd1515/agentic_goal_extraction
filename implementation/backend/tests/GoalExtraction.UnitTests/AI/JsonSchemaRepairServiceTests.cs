namespace GoalExtraction.UnitTests.AI;

using FluentAssertions;
using GoalExtraction.Infrastructure.AI;
using Xunit;

public class JsonSchemaRepairServiceTests
{
    private readonly JsonSchemaRepairService _service = new();

    [Fact]
    public void Parse_RawValidJson_ReturnsPopulatedResult()
    {
        // Arrange
        var rawJson = """
        {
          "summary": "Agreed to focus on testing and latency reduction.",
          "goals": [
            {
              "title": "Increase unit test coverage to 80%",
              "description": "Improve unit test coverage across application services.",
              "category": "TECHNICAL",
              "metric": "80% line coverage",
              "timeframe": "End of Q4",
              "priority": "HIGH"
            }
          ]
        }
        """;

        // Act
        var result = _service.Parse(rawJson);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Summary.Should().Be("Agreed to focus on testing and latency reduction.");
        result.Goals.Should().HaveCount(1);
        result.Goals[0].Title.Should().Be("Increase unit test coverage to 80%");
        result.Goals[0].Category.Should().Be("TECHNICAL");
        result.Goals[0].Priority.Should().Be("HIGH");
        result.Goals[0].Metric.Should().Be("80% line coverage");
        result.Goals[0].Timeframe.Should().Be("End of Q4");
    }

    [Fact]
    public void Parse_MarkdownFencedJson_StripsFencesAndParses()
    {
        // Arrange
        var markdownFenced = """
        ```json
        {
          "summary": "Review completed successfully.",
          "goals": [
            {
              "title": "Migrate auth service to OAuth2",
              "description": "Modernize authentication stack.",
              "category": "PROJECT",
              "metric": "100% services migrated",
              "timeframe": "Q1 2027",
              "priority": "MEDIUM"
            }
          ]
        }
        ```
        """;

        // Act
        var result = _service.Parse(markdownFenced);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Summary.Should().Be("Review completed successfully.");
        result.Goals.Should().HaveCount(1);
        result.Goals[0].Title.Should().Be("Migrate auth service to OAuth2");
    }

    [Fact]
    public void Parse_MarkdownFencedWithoutJsonTag_StripsFencesAndParses()
    {
        // Arrange
        var markdownFenced = """
        ```
        {
          "summary": "Test without json tag",
          "goals": []
        }
        ```
        """;

        // Act
        var result = _service.Parse(markdownFenced);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Summary.Should().Be("Test without json tag");
        result.Goals.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MarkdownFencedWithPreambleAndPostamble_StripsCleanlyAndParses()
    {
        // Arrange
        var input = """
        Here is the JSON result you asked for:
        ```json
        {
          "summary": "Meeting with Marcus concluded with 1 commitment.",
          "goals": [
            {
              "title": "Zero-downtime deployment",
              "description": "Ensure zero downtime during API rollout.",
              "category": "TECHNICAL",
              "metric": "100% uptime in monitoring",
              "timeframe": "End of Q3",
              "priority": "HIGH"
            }
          ]
        }
        ```
        Let me know if you need anything else!
        """;

        // Act
        var result = _service.Parse(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Summary.Should().Be("Meeting with Marcus concluded with 1 commitment.");
        result.Goals.Should().HaveCount(1);
        result.Goals[0].Title.Should().Be("Zero-downtime deployment");
    }

    [Fact]
    public void Parse_PreambleWithCurlyBracesAndFencedJson_ParsesInnerJsonCorrectly()
    {
        // Arrange
        var input = """
        Context for employee {id: 12345, name: "Marcus"}:
        ```json
        {
          "summary": "Targeted sprint goals established.",
          "goals": []
        }
        ```
        End of report.
        """;

        // Act
        var result = _service.Parse(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Summary.Should().Be("Targeted sprint goals established.");
        result.Goals.Should().BeEmpty();
    }

    [Fact]
    public void Parse_RawJsonWithPreambleAndPostambleWithoutFences_ParsesCorrectly()
    {
        // Arrange
        var input = """
        Analysis Output:
        {
          "summary": "Unfenced output with preamble",
          "goals": []
        }
        Done.
        """;

        // Act
        var result = _service.Parse(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Summary.Should().Be("Unfenced output with preamble");
        result.Goals.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_NullOrWhitespace_ThrowsTypedException(string? input)
    {
        var act = () => _service.Parse(input!);
        act.Should().Throw<JsonSchemaValidationException>();
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsTypedException()
    {
        // Arrange
        var malformedJson = "{ \"summary\": \"broken\", \"goals\": [ { title: missing_quotes } ] }";

        // Act & Assert
        var act = () => _service.Parse(malformedJson);
        act.Should().Throw<JsonSchemaValidationException>()
           .WithMessage("*Malformed JSON*");
    }

    [Fact]
    public void Parse_MissingSummary_ThrowsTypedException()
    {
        // Arrange
        var missingSummaryJson = "{ \"goals\": [] }";

        // Act & Assert
        var act = () => _service.Parse(missingSummaryJson);
        act.Should().Throw<JsonSchemaValidationException>()
           .WithMessage("*'summary'*");
    }

    [Fact]
    public void Parse_MissingGoals_ThrowsTypedException()
    {
        // Arrange
        var missingGoalsJson = "{ \"summary\": \"Valid summary\" }";

        // Act & Assert
        var act = () => _service.Parse(missingGoalsJson);
        act.Should().Throw<JsonSchemaValidationException>()
           .WithMessage("*'goals'*");
    }

    [Fact]
    public void Parse_GoalWithMissingTitle_ThrowsTypedException()
    {
        // Arrange
        var invalidGoalJson = """
        {
          "summary": "Valid summary",
          "goals": [
            {
              "description": "No title provided"
            }
          ]
        }
        """;

        // Act & Assert
        var act = () => _service.Parse(invalidGoalJson);
        act.Should().Throw<JsonSchemaValidationException>()
           .WithMessage("*title*");
    }
}
