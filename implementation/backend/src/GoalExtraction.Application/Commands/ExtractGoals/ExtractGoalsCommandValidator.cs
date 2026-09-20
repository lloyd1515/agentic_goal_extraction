namespace GoalExtraction.Application.Commands.ExtractGoals;

using FluentValidation;

public class ExtractGoalsCommandValidator : AbstractValidator<ExtractGoalsCommand>
{
    public ExtractGoalsCommandValidator()
    {
        RuleFor(x => x.Transcript)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Transcript cannot be empty.")
            .Must(t => (t ?? string.Empty).Trim().Length >= 20)
                .WithMessage("Minimum 20 characters required.")
            .Must(t => (t ?? string.Empty).Trim().Length <= 32000)
                .WithMessage("Maximum 32,000 characters allowed.");
    }
}
