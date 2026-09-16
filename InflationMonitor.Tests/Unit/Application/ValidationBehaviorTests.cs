using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using InflationMonitor.Application.Common.Behaviors;
using MediatR;
using Moq;

namespace InflationMonitor.Tests.Unit.Application {
    /// <summary>
    /// Unit tests for the MediatR <see cref="ValidationBehavior{TRequest, TResponse}"/> pipeline step.
    /// Verifies behavior execution with valid payloads and exception throwing on validation failures.
    /// </summary>
    public class ValidationBehaviorTests {
        public record TestQuery : IRequest<string>;

        /// <summary>
        /// Verifies that the pipeline continues execution when there are no validation errors.
        /// </summary>
        [Fact]
        public async Task Handle_WhenValidationSucceeds_ShouldCallNextDelegate() {
            // Arrange
            var mockValidator = new Mock<IValidator<TestQuery>>();
            mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestQuery>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var behavior = new ValidationBehavior<TestQuery, string>([mockValidator.Object]);
            var nextCalled = false;

            RequestHandlerDelegate<string> next = (_) => { 
                nextCalled = true;
                return Task.FromResult("Success");
            };

            // Act
            var result = await behavior.Handle(new TestQuery(), next, CancellationToken.None);

            // Assert
            result.Should().Be("Success");
            nextCalled.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that when a validation error occurs, the behavior throws a <see cref="ValidationException"/> and halts the pipeline.
        /// </summary>
        [Fact]
        public async Task Handle_WhenValidationFails_ShouldThrowValidationExceptionAndNotCallNext() {
            // Arrange
            var failures = new List<ValidationFailure> {
                new("Amount", "Amount must be greater than zero.")
            };

            var mockValidator = new Mock<IValidator<TestQuery>>();
            mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestQuery>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            var behavior = new ValidationBehavior<TestQuery, string>([mockValidator.Object]);
            var nextCalled = false;

            RequestHandlerDelegate<string> next = (_) => { 
                nextCalled = true;
                return Task.FromResult("Success");
            };

            // Act
            Func<Task> act = async () => await behavior.Handle(new TestQuery(), next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            nextCalled.Should().BeFalse();
        }
    }
}