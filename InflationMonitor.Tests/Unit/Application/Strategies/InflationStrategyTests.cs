using FluentAssertions;
using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Strategies;
using InflationMonitor.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InflationMonitor.Tests.Unit.Application.Strategies {
    /// <summary>
    /// Unit tests for the <see cref="InflationStrategy"/> class.
    /// Validates compound inflation calculations, gaps in database records, 
    /// boundary date warnings, and memory caching functionality.
    /// </summary>
    public class InflationStrategyTests : StrategyTestBase {
        private readonly InflationStrategy _strategy;

        public InflationStrategyTests() {
            _strategy = new InflationStrategy(DbContext, MemoryCache);
        }

        /// <summary>
        /// Verifies that compound inflation rate calculation is mathematically correct 
        /// when complete historical data is present in the database.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenDataInDb_ShouldCalculateCompoundInflationCorrectly() {
            // Arrange
            var startDate = new DateOnly(2023, 1, 1);
            var endDate = new DateOnly(2023, 3, 1);
            const decimal amount = 1000m;

            var inflationRates = new List<InflationRate> {
                new(new DateOnly(2023, 1, 1), 1.01m),
                new(new DateOnly(2023, 2, 1), 1.02m),
                new(new DateOnly(2023, 3, 1), 1.005m)
            };

            DbContext.InflationRates.AddRange(inflationRates);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                [], startDate, endDate, amount, CancellationToken.None);

            // Assert: 1000 * (1.01 * 1.02 * 1.005) = 1035.351 -> 1035.35
            result.Equivalents.Should().ContainKey(InflationConstants.Codes.Cpi);
            result.Equivalents[InflationConstants.Codes.Cpi].Should().Be(1035.35m);
            result.Warnings.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that when rate data is incomplete for the requested date range,
        /// the returned equivalent for CPI is null and a warning is produced.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenPeriodIsMissingInDb_ShouldReturnNullAndWarning() {
            // Arrange:
            var startDate = new DateOnly(2023, 1, 1);
            var endDate = new DateOnly(2023, 3, 1);

            var partialRates = new List<InflationRate> {
                new(new DateOnly(2023, 1, 1), 1.01m),
                // 2023, 2, 1 : intentionally not entered 
                new(new DateOnly(2023, 3, 1), 1.005m)
            };

            DbContext.InflationRates.AddRange(partialRates);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                [], startDate, endDate, 1000m, CancellationToken.None);

            // Assert
            result.Equivalents[InflationConstants.Codes.Cpi].Should().BeNull();
            result.Warnings.Should().HaveCount(1);
            result.Warnings[0].Should().Contain("incomplete or unavailable");
        }

        /// <summary>
        /// Verifies that requesting a start date prior to the supported minimum threshold
        /// produces a boundary warning message.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenStartDateIsBeforeMinSupported_ShouldReturnSpecificWarning() {
            // Arrange
            var startDate = new DateOnly(1999, 1, 1);
            var endDate = new DateOnly(2000, 2, 1);

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                [], startDate, endDate, 1000m, CancellationToken.None);

            // Assert
            result.Equivalents[InflationConstants.Codes.Cpi].Should().BeNull();
            result.Warnings.Should().ContainSingle()
                .Which.Should().Contain("available only starting from 2000-01");
        }

        /// <summary>
        /// Verifies that requesting an end date exceeding the maximum date available in the database
        /// produces an upper boundary warning message.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenEndDateExceedsMaxInDb_ShouldReturnUpperBoundaryWarning() {
            // Arrange: Заполняем БД данными только до марта 2023 года
            var startDate = new DateOnly(2023, 1, 1);
            var endDate = new DateOnly(2023, 5, 1); 

            var inflationRates = new List<InflationRate> {
                new(new DateOnly(2023, 1, 1), 1.01m),
                new(new DateOnly(2023, 2, 1), 1.02m),
                new(new DateOnly(2023, 3, 1), 1.005m)
            };

            DbContext.InflationRates.AddRange(inflationRates);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                [], startDate, endDate, 1000m, CancellationToken.None);

            // Assert
            result.Equivalents[InflationConstants.Codes.Cpi].Should().BeNull();
            result.Warnings.Should().HaveCount(1);
            result.Warnings[0].Should().Contain("2023-03");
        }

        /// <summary>
        /// Verifies that rate entries loaded from the database are stored in memory cache 
        /// and reused on subsequent execution without querying the database.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenCalledRepeatedly_ShouldServeFromCache() {
            // Arrange
            var startDate = new DateOnly(2023, 1, 1);
            var endDate = new DateOnly(2023, 1, 1);
            const decimal amount = 1000m;

            var rate = new InflationRate(startDate, 1.03m);
            DbContext.InflationRates.Add(rate);
            await DbContext.SaveChangesAsync();

            // Act 1: The first call loads data from the database and warms up the cache.
            var firstResult = await _strategy.CalculateEquivalentsAsync(
                [], startDate, endDate, amount, CancellationToken.None);

            // Clear the database to ensure that the repeated request does not depend on the database.
            DbContext.InflationRates.RemoveRange(DbContext.InflationRates);
            await DbContext.SaveChangesAsync();

            // Act 2: The second call should retrieve data from the populated cache.
            var secondResult = await _strategy.CalculateEquivalentsAsync(
                [], startDate, endDate, amount, CancellationToken.None);

            // Assert
            firstResult.Equivalents[InflationConstants.Codes.Cpi].Should().Be(1030.00m);
            secondResult.Equivalents[InflationConstants.Codes.Cpi].Should().Be(1030.00m);
            secondResult.Warnings.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that passing a cancelled CancellationToken throws OperationCanceledException.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenCancelled_ShouldThrowOperationCanceledException() {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel(); 

            // Act
            Func<Task> act = async () => await _strategy.CalculateEquivalentsAsync(
                [],
                new DateOnly(2023, 1, 1),
                new DateOnly(2023, 3, 1),
                1000m,
                cts.Token); 

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>(); 
        }
    }
}