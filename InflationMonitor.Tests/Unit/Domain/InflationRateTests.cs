using FluentAssertions;
using InflationMonitor.Domain.Entities;
using InflationMonitor.Domain.Exceptions;

namespace InflationMonitor.Tests.Unit.Domain {
    /// <summary>
    /// Unit tests for the <see cref="InflationRate"/> domain entity.
    /// Verifies domain invariant enforcement, input validation, and date normalization rules.
    /// </summary>
    public class InflationRateTests {
        /// <summary>
        /// Verifies that valid constructor arguments correctly initialize the domain entity
        /// and normalize the date to the 1st day of the month.
        /// </summary>
        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializeEntityAndNormalizeDate() {
            // Arrange
            var inputDate = new DateOnly(2023, 5, 20);
            var expectedDate = new DateOnly(2023, 5, 1);
            const decimal rateInput = 1.008m;

            // Act
            var inflationRate = new InflationRate(inputDate, rateInput);

            // Assert
            inflationRate.Date.Should().Be(expectedDate);
            inflationRate.Rate.Should().Be(rateInput);
        }

        /// <summary>
        /// Verifies that passing a date prior to January 2000 throws an <see cref="InvalidHistoricalPeriodException"/>.
        /// </summary>
        [Fact]
        public void Constructor_WhenDateIsBeforeJanuary2000_ShouldThrowInvalidHistoricalPeriodException() {
            // Arrange
            var invalidDate = new DateOnly(1999, 12, 31);

            // Act
            Action act = () => _ = new InflationRate(invalidDate, 1.01m);

            // Assert
            act.Should().Throw<InvalidHistoricalPeriodException>()
               .WithMessage("*January 2000*");
        }

        /// <summary>
        /// Verifies that passing a negative inflation rate throws a <see cref="DomainArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void Constructor_WhenRateIsNegative_ShouldThrowDomainArgumentOutOfRangeException() {
            // Arrange
            var validDate = new DateOnly(2023, 1, 1);
            const decimal negativeRate = -0.05m;

            // Act
            Action act = () => _ = new InflationRate(validDate, negativeRate);

            // Assert
            act.Should().Throw<DomainArgumentOutOfRangeException>()
               .Where(ex => ex.ParamName == "rate")
               .WithMessage("*cannot be negative*");
        }
    }
}