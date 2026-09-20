namespace GoalExtraction.UnitTests.Commands;

using FluentAssertions;
using FluentValidation.TestHelper;
using GoalExtraction.Application.Commands.SaveGoalsBatch;
using GoalExtraction.Application.DTOs;
using Xunit;

public class SaveGoalsBatchCommandValidatorTests
{
    private readonly SaveGoalsBatchCommandValidator _validator = new();

    [Fact]
    public void Validator_EmptyGoalsList_FailsWithAtLeastOneGoalMustBeProvided()
    {
        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.NewGuid(),
            SourceTranscriptHash: "abc123hash",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>());

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Goals)
            .WithErrorMessage("At least one goal must be provided.");
    }

    [Fact]
    public void Validator_EmptyEmployeeId_FailsWithEmployeeIdRequired()
    {
        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.Empty,
            SourceTranscriptHash: "abc123hash",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Deliver Q3 Milestones",
                    Description = "Ensure on-time delivery of the search index microservice.",
                    Category = "PERFORMANCE",
                    Priority = "HIGH"
                }
            });

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.EmployeeId)
            .WithErrorMessage("EmployeeId cannot be empty.");
    }

    [Fact]
    public void Validator_GoalItemWithEmptyTitle_FailsWithTitleCannotBeEmpty()
    {
        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.NewGuid(),
            SourceTranscriptHash: "hash123",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "",
                    Description = "Valid description exceeding ten characters.",
                    Category = "PERFORMANCE",
                    Priority = "MEDIUM"
                }
            });

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Goals[0].Title")
            .WithErrorMessage("Title cannot be empty.");
    }

    [Fact]
    public void Validator_GoalItemWithInvalidPriority_FailsWithAllowedValuesMessage()
    {
        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.NewGuid(),
            SourceTranscriptHash: "hash123",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Reduce memory footprint",
                    Description = "Optimize memory allocations across all microservices.",
                    Category = "TECHNICAL",
                    Priority = "URGENT" // Invalid!
                }
            });

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Goals[0].Priority")
            .WithErrorMessage("Invalid goal priority. Allowed values: HIGH, MEDIUM, LOW.");
    }

    [Fact]
    public void Validator_GoalItemWithInvalidCategory_Fails()
    {
        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.NewGuid(),
            SourceTranscriptHash: "hash123",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Reduce memory footprint",
                    Description = "Optimize memory allocations across all microservices.",
                    Category = "RANDOM_CATEGORY",
                    Priority = "HIGH"
                }
            });

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Goals[0].Category");
    }

    [Fact]
    public void Validator_ValidCommand_PassesValidation()
    {
        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.NewGuid(),
            SourceTranscriptHash: "hash123456",
            ReviewerId: Guid.NewGuid(),
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Migrate to .NET 8 LTS",
                    Description = "Complete migration of the payment service to .NET 8 before end of quarter.",
                    Category = "TECHNICAL",
                    Metric = "Zero build errors, 90% test coverage",
                    Timeframe = "Q4 2026",
                    Priority = "HIGH",
                    Provenance = "AI_ORIGINAL"
                },
                new()
                {
                    Title = "Improve API Latency",
                    Description = "Reduce 95th percentile response times below 150ms through response caching.",
                    Category = "PERFORMANCE",
                    Metric = "p95 < 150ms",
                    Timeframe = "End of month",
                    Priority = "MEDIUM",
                    Provenance = "MANUAL"
                }
            },
            CorrelationId: "corr-1234");

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_GoalItemWithTitleShorterThan5Chars_FailsWithLengthError()
    {
        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.NewGuid(),
            SourceTranscriptHash: "hash123",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = "Four", // 4 chars - less than 5
                    Description = "Valid description exceeding ten characters.",
                    Category = "PERFORMANCE",
                    Priority = "MEDIUM"
                }
            });

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Goals[0].Title")
            .WithErrorMessage("Title must be between 5 and 120 characters.");
    }

    [Fact]
    public void Validator_GoalItemWithTitleLongerThan120Chars_FailsWithLengthError()
    {
        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.NewGuid(),
            SourceTranscriptHash: "hash123",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>
            {
                new()
                {
                    Title = new string('A', 121), // 121 chars - greater than 120
                    Description = "Valid description exceeding ten characters.",
                    Category = "PERFORMANCE",
                    Priority = "MEDIUM"
                }
            });

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Goals[0].Title")
            .WithErrorMessage("Title must be between 5 and 120 characters.");
    }

    [Fact]
    public void Validator_MoreThan50Goals_FailsWithMaximum50GoalsError()
    {
        var goals = Enumerable.Range(1, 51).Select(i => new SaveGoalItemDto
        {
            Title = $"Valid Goal Title {i}",
            Description = "A valid detailed description exceeding ten chars.",
            Category = "PERFORMANCE",
            Priority = "HIGH"
        }).ToList();

        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.NewGuid(),
            SourceTranscriptHash: "hash123",
            ReviewerId: null,
            Goals: goals);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Goals)
            .WithErrorMessage("Maximum 50 goals can be saved in a single batch.");
    }

    [Fact]
    public void Validator_Exactly50Goals_PassesValidation()
    {
        var goals = Enumerable.Range(1, 50).Select(i => new SaveGoalItemDto
        {
            Title = $"Valid Goal Title {i:D2}",
            Description = "A valid detailed description exceeding ten chars.",
            Category = "PERFORMANCE",
            Priority = "MEDIUM"
        }).ToList();

        var command = new SaveGoalsBatchCommand(
            EmployeeId: Guid.NewGuid(),
            SourceTranscriptHash: "hash123",
            ReviewerId: null,
            Goals: goals);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }
}
