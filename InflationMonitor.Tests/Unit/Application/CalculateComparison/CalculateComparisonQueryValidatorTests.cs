using FluentValidation.TestHelper;
using InflationMonitor.Application.Queries.CalculateComparison;

namespace InflationMonitor.Tests.Unit.Application.CalculateComparison {
    /// <summary>
    /// Unit tests for the <see cref="CalculateComparisonQueryValidator"/> FluentValidation class.
    /// Verifies input parameters, lower historical date boundaries, and future date constraints.
    /// </summary>
    public class CalculateComparisonQueryValidatorTests {
        private readonly CalculateComparisonQueryValidator _validator = new();

        /// <summary>
        /// Verifies that a valid query payload passes validation without any errors.
        /// </summary>
        [Fact]
        public void Validate_WhenQueryIsValid_ShouldNotHaveAnyValidationErrors() {
            // Arrange
            var query = new CalculateComparisonQuery(
                new DateOnly(2023, 1, 1),
                new DateOnly(2023, 5, 1),
                1000m);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        /// <summary>
        /// Verifies that zero or negative monetary amounts produce validation errors for the Amount property.
        /// </summary>
        /// <param name="invalidAmount">The invalid monetary amount to test.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-500)]
        public void Validate_WhenAmountIsZeroOrNegative_ShouldHaveValidationErrorForAmount(decimal invalidAmount) {
            // Arrange
            var query = new CalculateComparisonQuery(
                new DateOnly(2023, 1, 1),
                new DateOnly(2023, 5, 1),
                invalidAmount);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Amount)
                  .WithErrorMessage("Amount must be greater than zero.");
        }

        /// <summary>
        /// Verifies that setting StartDate after EndDate produces a validation error for StartDate.
        /// </summary>
        [Fact]
        public void Validate_WhenStartDateIsAfterEndDate_ShouldHaveValidationErrorForStartDate() {
            // Arrange
            var query = new CalculateComparisonQuery(
                new DateOnly(2023, 6, 1),
                new DateOnly(2023, 1, 1),
                1000m);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                  .WithErrorMessage("StartDate cannot be later than EndDate.");
        }

        /// <summary>
        /// Verifies that dates prior to September 1996 produce a historical boundary validation error.
        /// </summary>
        [Fact]
        public void Validate_WhenStartDateIsBeforeSeptember1996_ShouldHaveValidationErrorForStartDate() {
            // Arrange
            var query = new CalculateComparisonQuery(
                new DateOnly(1996, 8, 1),
                new DateOnly(2023, 1, 1),
                1000m);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                .Where(e => e.ErrorMessage.Contains("September 2, 1996")); 
        }

        /// <summary>
        /// Verifies that an EndDate extending into future months produces a validation error.
        /// </summary>
        [Fact]
        public void Validate_WhenEndDateIsInFuture_ShouldHaveValidationErrorForEndDate() {
            // Arrange
            var futureDate = new DateOnly(DateTime.UtcNow.Year + 1, 1, 1);
            var query = new CalculateComparisonQuery(
                new DateOnly(2023, 1, 1),
                futureDate,
                1000m);

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndDate)
                  .WithErrorMessage("EndDate cannot be in the future.");
        }
    }
}