namespace GoalExtraction.Application.Commands.SaveGoalsBatch;

using FluentValidation;
using GoalExtraction.Application.DTOs;
using GoalExtraction.Domain.Enums;

public class SaveGoalsBatchCommandValidator : AbstractValidator<SaveGoalsBatchCommand>
{
    public SaveGoalsBatchCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("EmployeeId cannot be empty.");

        RuleFor(x => x.Goals)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("At least one goal must be provided.")
            .NotEmpty().WithMessage("At least one goal must be provided.")
            .Must(goals => goals.Count <= 50)
                .WithMessage("Maximum 50 goals can be saved in a single batch.");

        RuleForEach(x => x.Goals).SetValidator(new SaveGoalItemDtoValidator());
    }
}

public class SaveGoalItemDtoValidator : AbstractValidator<SaveGoalItemDto>
{
    public SaveGoalItemDtoValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Title cannot be empty.")
            .Length(5, 120).WithMessage("Title must be between 5 and 120 characters.");

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Description cannot be empty.")
            .Length(10, 1000).WithMessage("Description must be between 10 and 1000 characters.");

        RuleFor(x => x.Category)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Category cannot be empty.")
            .Must(BeValidCategory)
                .WithMessage("Invalid goal category. Allowed values: PERFORMANCE, DEVELOPMENT, PROJECT, TECHNICAL.");

        RuleFor(x => x.Priority)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Priority cannot be empty.")
            .Must(BeValidPriority)
                .WithMessage("Invalid goal priority. Allowed values: HIGH, MEDIUM, LOW.");

        RuleFor(x => x.Provenance)
            .Must(BeValidProvenance)
                .When(x => !string.IsNullOrWhiteSpace(x.Provenance))
                .WithMessage("Invalid goal provenance. Allowed values: AI_ORIGINAL, AI_MODIFIED, MANUAL.");

        RuleFor(x => x.Metric)
            .MaximumLength(500).WithMessage("Metric cannot exceed 500 characters.");

        RuleFor(x => x.Timeframe)
            .MaximumLength(100).WithMessage("Timeframe cannot exceed 100 characters.");
    }

    private static bool BeValidCategory(string? category) =>
        !string.IsNullOrWhiteSpace(category) && Enum.TryParse<GoalCategory>(category.Trim(), true, out _);

    private static bool BeValidPriority(string? priority) =>
        !string.IsNullOrWhiteSpace(priority) && Enum.TryParse<GoalPriority>(priority.Trim(), true, out _);

    private static bool BeValidProvenance(string? provenance) =>
        string.IsNullOrWhiteSpace(provenance) || Enum.TryParse<GoalProvenance>(provenance.Trim(), true, out _);
}
