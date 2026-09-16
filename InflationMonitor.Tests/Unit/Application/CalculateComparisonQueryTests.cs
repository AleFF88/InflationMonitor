using FluentAssertions;
using InflationMonitor.Application.Queries.CalculateComparison;

namespace InflationMonitor.Tests.Unit.Application {
    /// <summary>
    /// Unit tests for the <see cref="CalculateComparisonQuery"/> request object.
    /// Verifies date normalization rules during query construction.
    /// </summary>
    public class CalculateComparisonQueryTests {
        /// <summary>
        /// Verifies that any arbitrary start and end dates provided to the query constructor
        /// are automatically normalized to the 1st day of their respective months.
        /// </summary>
        [Fact]
        public void Constructor_WhenDatesProvided_ShouldNormalizeStartAndEndDatesToFirstDayOfMonth() {
            // Arrange
            var startDateInput = new DateOnly(2023, 3, 15);
            var endDateInput = new DateOnly(2023, 8, 28);

            var expectedStartDate = new DateOnly(2023, 3, 1);
            var expectedEndDate = new DateOnly(2023, 8, 1);
            const decimal amount = 1000m;

            // Act
            var query = new CalculateComparisonQuery(startDateInput, endDateInput, amount);

            // Assert
            query.StartDate.Should().Be(expectedStartDate);
            query.EndDate.Should().Be(expectedEndDate);
            query.Amount.Should().Be(amount);
        }
    }
}