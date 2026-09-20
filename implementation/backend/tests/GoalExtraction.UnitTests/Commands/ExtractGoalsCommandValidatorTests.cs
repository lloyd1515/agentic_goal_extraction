namespace GoalExtraction.UnitTests.Commands;

using FluentAssertions;
using FluentValidation.TestHelper;
using GoalExtraction.Application.Commands.ExtractGoals;
using Xunit;

public class ExtractGoalsCommandValidatorTests
{
    private readonly ExtractGoalsCommandValidator _validator = new();

    [Fact]
    public void Validator_TranscriptWith19Chars_FailsWithMinimum20CharactersRequired()
    {
        // 19 characters
        var command = new ExtractGoalsCommand("1234567890123456789");

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Transcript)
            .WithErrorMessage("Minimum 20 characters required.");
    }

    [Fact]
    public void Validator_TranscriptWith32001Chars_FailsWithMaximum32000CharactersAllowed()
    {
        // 32001 characters
        var command = new ExtractGoalsCommand(new string('a', 32001));

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Transcript)
            .WithErrorMessage("Maximum 32,000 characters allowed.");
    }

    [Fact]
    public void Validator_ValidTranscriptWith150Chars_ValidationSucceeds()
    {
        var transcript = "In this quarter, our key objective is to improve system performance by 30%, migrate existing microservices to .NET 8, and reduce p99 latency under 200ms.";
        transcript.Length.Should().BeGreaterThan(20).And.BeLessThan(32000);

        var command = new ExtractGoalsCommand(transcript, Guid.NewGuid(), "Engineering", "corr-123");

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_EmptyOrNullTranscript_FailsWithEmptyError()
    {
        var emptyCommand = new ExtractGoalsCommand("");
        var emptyResult = _validator.TestValidate(emptyCommand);
        emptyResult.IsValid.Should().BeFalse();
        emptyResult.ShouldHaveValidationErrorFor(x => x.Transcript)
            .WithErrorMessage("Transcript cannot be empty.");

        var whitespaceCommand = new ExtractGoalsCommand("    ");
        var whitespaceResult = _validator.TestValidate(whitespaceCommand);
        whitespaceResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_TranscriptWithExactly20Chars_ValidationSucceeds()
    {
        var command = new ExtractGoalsCommand(new string('a', 20));
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_TranscriptWithExactly32000Chars_ValidationSucceeds()
    {
        var command = new ExtractGoalsCommand(new string('a', 32000));
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeTrue();
    }
}
