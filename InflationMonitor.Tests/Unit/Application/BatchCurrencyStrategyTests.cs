using FluentAssertions;
using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Strategies;
using InflationMonitor.Domain.Entities;

namespace InflationMonitor.Tests.Unit.Application {
    /// <summary>
    /// Unit tests for the <see cref="BatchCurrencyStrategy"/> class.
    /// Validates currency conversions using initial and target exchange rates, gap handling, 
    /// boundary date checking, and memory caching behavior.
    /// </summary>
    public class BatchCurrencyStrategyTests : StrategyTestBase {
        private readonly BatchCurrencyStrategy _strategy;

        public BatchCurrencyStrategyTests() {
            _strategy = new BatchCurrencyStrategy(DbContext, MemoryCache);
        }

        /// <summary>
        /// Verifies that currency conversion correctly calculates equivalents 
        /// using ratios of initial and target exchange rates for multiple currencies.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenRatesExistInDb_ShouldCalculateCurrencyEquivalentsCorrectly() {
            // Arrange
            var startDate = new DateOnly(2023, 1, 1);
            var endDate = new DateOnly(2023, 3, 1);
            const decimal amount = 1000m;
            var requestedCurrencies = new[] { CurrencyConstants.Codes.Usd, CurrencyConstants.Codes.Eur };

            var exchangeRates = new List<ExchangeRate> {
                new(CurrencyConstants.Codes.Usd, startDate, 36.5m),
                new(CurrencyConstants.Codes.Eur, startDate, 40.0m),
                new(CurrencyConstants.Codes.Usd, endDate, 40.0m),
                new(CurrencyConstants.Codes.Eur, endDate, 42.0m)
            };

            DbContext.ExchangeRates.AddRange(exchangeRates);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                requestedCurrencies, startDate, endDate, amount, CancellationToken.None);

            // Assert: 1000 * (40.0 / 36.5) = 1095.89; 1000 * (42.0 / 40.0) = 1050.00 
            result.Equivalents.Should().ContainKey(CurrencyConstants.Codes.Usd);
            result.Equivalents.Should().ContainKey(CurrencyConstants.Codes.Eur);
            result.Equivalents[CurrencyConstants.Codes.Usd].Should().Be(1095.89m); 
            result.Equivalents[CurrencyConstants.Codes.Eur].Should().Be(1050.00m); 
            result.Warnings.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that when exchange rate data is incomplete for a requested currency on either boundary date,
        /// the returned equivalent for that currency is null and a warning is produced.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenRateIsMissingForCurrency_ShouldReturnNullAndWarning() {
            // Arrangeт
            var startDate = new DateOnly(2023, 1, 1);
            var endDate = new DateOnly(2023, 3, 1);
            var requestedCurrencies = new[] { CurrencyConstants.Codes.Usd, CurrencyConstants.Codes.Eur };

            var partialRates = new List<ExchangeRate> {
                new(CurrencyConstants.Codes.Usd, startDate, 36.5m),
                new(CurrencyConstants.Codes.Eur, startDate, 40.0m),
                new(CurrencyConstants.Codes.Usd, endDate, 40.0m)
                // no data for EUR on endDate
            };

            DbContext.ExchangeRates.AddRange(partialRates);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                requestedCurrencies, startDate, endDate, 1000m, CancellationToken.None);

            // Assert
            result.Equivalents[CurrencyConstants.Codes.Usd].Should().Be(1095.89m);
            result.Equivalents[CurrencyConstants.Codes.Eur].Should().BeNull();
            result.Warnings.Should().HaveCount(1);
            result.Warnings[0].Should().Contain("is available only up to");
        }

        /// <summary>
        /// Verifies that requesting a start date prior to the supported minimum threshold
        /// produces a boundary warning message.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenStartDateIsBeforeMinSupported_ShouldReturnSpecificWarning() {
            // Arrange
            var startDate = new DateOnly(1995, 1, 1);
            var endDate = new DateOnly(2000, 2, 1);
            var requestedCurrencies = new[] { CurrencyConstants.Codes.Usd };

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                requestedCurrencies, startDate, endDate, 1000m, CancellationToken.None);

            // Assert
            result.Equivalents[CurrencyConstants.Codes.Usd].Should().BeNull();
            result.Warnings.Should().ContainSingle()
                .Which.Should().Contain("available only starting from");
        }

        /// <summary>
        /// Verifies that requesting an end date exceeding the maximum date available in the database
        /// produces an upper boundary warning message.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenEndDateExceedsMaxInDb_ShouldReturnUpperBoundaryWarning() {
            // Arrange
            var startDate = new DateOnly(2023, 1, 1);
            var endDate = new DateOnly(2023, 5, 1);
            var requestedCurrencies = new[] { CurrencyConstants.Codes.Usd };

            var exchangeRates = new List<ExchangeRate> {
                new(CurrencyConstants.Codes.Usd, startDate, 36.5m),
                new(CurrencyConstants.Codes.Usd, new DateOnly(2023, 2, 1), 38.0m),
                new(CurrencyConstants.Codes.Usd, new DateOnly(2023, 3, 1), 38.0m)
            };

            DbContext.ExchangeRates.AddRange(exchangeRates);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                requestedCurrencies, startDate, endDate, 1000m, CancellationToken.None);

            // Assert
            result.Equivalents[CurrencyConstants.Codes.Usd].Should().BeNull();
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
            var endDate = new DateOnly(2023, 3, 1);
            const decimal amount = 1000m;
            var requestedCurrencies = new[] { CurrencyConstants.Codes.Usd };

            var rates = new List<ExchangeRate> {
                new(CurrencyConstants.Codes.Usd, startDate, 36.5m),
                new(CurrencyConstants.Codes.Usd, endDate, 40.0m)
            };

            DbContext.ExchangeRates.AddRange(rates);
            await DbContext.SaveChangesAsync();

            // Act 1: The first call loads data from the database and warms up the cache.
            var firstResult = await _strategy.CalculateEquivalentsAsync(
                requestedCurrencies, startDate, endDate, amount, CancellationToken.None);

            // Clear the database to ensure that the repeated request does not depend on the database.
            DbContext.ExchangeRates.RemoveRange(DbContext.ExchangeRates);
            await DbContext.SaveChangesAsync();

            // Act 2: The second call should retrieve data from the populated cache.
            var secondResult = await _strategy.CalculateEquivalentsAsync(
                requestedCurrencies, startDate, endDate, amount, CancellationToken.None);

            // Assert
            firstResult.Equivalents[CurrencyConstants.Codes.Usd].Should().Be(1095.89m);
            secondResult.Equivalents[CurrencyConstants.Codes.Usd].Should().Be(1095.89m);
            secondResult.Warnings.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that passing an empty list of requested currencies returns an empty
        /// equivalents dictionary without throwing.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenRequestedCurrenciesIsEmpty_ShouldReturnEmptyEquivalents() {
            // Arrange
            var startDate = new DateOnly(2023, 1, 1);
            var endDate = new DateOnly(2023, 3, 1);

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                Array.Empty<string>(), startDate, endDate, 1000m, CancellationToken.None);

            // Assert
            result.Equivalents.Should().BeEmpty(); 
            result.Warnings.Should().BeEmpty(); 
        }

        /// <summary>
        /// Verifies that currency codes provided in lowercase are handled case-insensitively.
        /// </summary>
        [Fact]
        public async Task CalculateEquivalentsAsync_WhenCurrencyCodesAreLowercase_ShouldCalculateCorrectly() {
            // Arrange
            var startDate = new DateOnly(2023, 1, 1);
            var endDate = new DateOnly(2023, 3, 1);
            var exchangeRates = new List<ExchangeRate> {
                new(CurrencyConstants.Codes.Usd, startDate, 36.5m),
                new(CurrencyConstants.Codes.Usd, endDate, 40.0m)
            };

            DbContext.ExchangeRates.AddRange(exchangeRates);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _strategy.CalculateEquivalentsAsync(
                new[] { "usd" }, startDate, endDate, 1000m, CancellationToken.None); 

            // Assert
            result.Equivalents.Should().ContainKey(CurrencyConstants.Codes.Usd); 
            result.Equivalents[CurrencyConstants.Codes.Usd].Should().Be(1095.89m); 
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
                new[] { CurrencyConstants.Codes.Usd },
                new DateOnly(2023, 1, 1),
                new DateOnly(2023, 3, 1),
                1000m,
                cts.Token); 

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>(); 
        }
    }
}