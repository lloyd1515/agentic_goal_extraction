namespace GoalExtraction.UnitTests.Behaviors;

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using GoalExtraction.Application;
using GoalExtraction.Application.Commands.ExtractGoals;
using GoalExtraction.Application.Commands.SaveGoalsBatch;
using GoalExtraction.Application.Common.Behaviors;
using GoalExtraction.Application.DTOs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

public class ValidationBehaviorTests
{
    public class SampleCommand : IRequest<string>
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Handle_NoValidatorsRegistered_InvokesNextDelegate()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<SampleCommand>>();
        var behavior = new ValidationBehavior<SampleCommand, string>(validators);
        var command = new SampleCommand { Name = "Valid" };
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(command, () =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        }, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be("Success");
    }

    [Fact]
    public async Task Handle_ValidationPasses_InvokesNextDelegate()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<SampleCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<SampleCommand, string>(new[] { validatorMock.Object });
        var command = new SampleCommand { Name = "Valid" };
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(command, () =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        }, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be("Success");
    }

    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException_AndDoesNotCallNext()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name cannot be empty.")
        };

        var validatorMock = new Mock<IValidator<SampleCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behavior = new ValidationBehavior<SampleCommand, string>(new[] { validatorMock.Object });
        var command = new SampleCommand { Name = "" };
        var nextCalled = false;

        // Act
        var act = async () => await behavior.Handle(command, () =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        }, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Name" && e.ErrorMessage == "Name cannot be empty.");
        nextCalled.Should().BeFalse("Pipeline execution must stop when validation fails");
    }

    [Fact]
    public async Task MediatRPipeline_InvalidExtractGoalsCommand_ThrowsValidationException()
    {
        // Arrange: configure service collection with application services (MediatR + ValidationBehavior + Validators)
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Command with invalid transcript (< 20 chars)
        var invalidCommand = new ExtractGoalsCommand("Short transcript");

        // Act
        var act = async () => await mediator.Send(invalidCommand);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Transcript" && e.ErrorMessage.Contains("Minimum 20 characters"));
    }

    [Fact]
    public async Task MediatRPipeline_InvalidSaveGoalsBatchCommand_ThrowsValidationException()
    {
        // Arrange: configure service collection with application services
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Command with empty goals list and empty employeeId
        var invalidCommand = new SaveGoalsBatchCommand(
            EmployeeId: Guid.Empty,
            SourceTranscriptHash: "hash",
            ReviewerId: null,
            Goals: new List<SaveGoalItemDto>());

        // Act
        var act = async () => await mediator.Send(invalidCommand);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "EmployeeId");
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Goals");
    }
}
