using FluentAssertions;
using InflationMonitor.Domain.Entities;
using InflationMonitor.Domain.Exceptions;

namespace InflationMonitor.Tests.Unit.Domain {
    /// <summary>
    /// Unit tests for the <see cref="ExchangeRate"/> domain entity.
    /// Verifies domain invariant enforcement, input validation, and date/code normalization rules.
    /// </summary>
    public class ExchangeRateTests {
        /// <summary>
        /// Verifies that valid constructor arguments correctly initialize the domain entity,
        /// normalize the date to the 1st day of the month, and uppercase the currency code.
        /// </summary>
        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializeEntityAndNormalizeData() {
            // Arrange
            var inputDate = new DateOnly(2023, 5, 15);
            var expectedDate = new DateOnly(2023, 5, 1);
            const string currencyCodeInput = "usd";
            const decimal rateInput = 36.6m;

            // Act
            var exchangeRate = new ExchangeRate(currencyCodeInput, inputDate, rateInput);

            // Assert
            exchangeRate.CurrencyCode.Should().Be("USD");
            exchangeRate.Date.Should().Be(expectedDate);
            exchangeRate.Rate.Should().Be(rateInput);
        }

        /// <summary>
        /// Verifies that passing a date prior to September 1996 throws an <see cref="InvalidHistoricalPeriodException"/>.
        /// </summary>
        [Fact]
        public void Constructor_WhenDateIsBeforeSeptember1996_ShouldThrowInvalidHistoricalPeriodException() {
            // Arrange
            var invalidDate = new DateOnly(1996, 8, 31);

            // Act
            Action act = () => _ = new ExchangeRate("USD", invalidDate, 1.76m);

            // Assert
            act.Should().Throw<InvalidHistoricalPeriodException>()
               .WithMessage("*September 1996*");
        }

        /// <summary>
        /// Verifies that passing a zero or negative exchange rate throws a <see cref="DomainArgumentOutOfRangeException"/>.
        /// </summary>
        /// <param name="invalidRate">The invalid zero or negative exchange rate value to test.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1.5)]
        public void Constructor_WhenRateIsZeroOrNegative_ShouldThrowDomainArgumentOutOfRangeException(decimal invalidRate) {
            // Arrange
            var validDate = new DateOnly(2023, 1, 1);

            // Act
            Action act = () => _ = new ExchangeRate("USD", validDate, invalidRate);

            // Assert
            act.Should().Throw<DomainArgumentOutOfRangeException>()
               .Where(ex => ex.ParamName == "rate")
               .WithMessage("*greater than zero*");
        }

        /// <summary>
        /// Verifies that passing a null, empty, or whitespace-only currency code throws a <see cref="DomainArgumentOutOfRangeException"/>.
        /// </summary>
        /// <param name="invalidCurrencyCode">The invalid currency code string to test.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WhenCurrencyCodeIsInvalid_ShouldThrowDomainArgumentOutOfRangeException(string? invalidCurrencyCode) {
            // Arrange
            var validDate = new DateOnly(2023, 1, 1);

            // Act
            Action act = () => _ = new ExchangeRate(invalidCurrencyCode!, validDate, 36.5m);

            // Assert
            act.Should().Throw<DomainArgumentOutOfRangeException>()
               .Where(ex => ex.ParamName == "currencyCode")
               .WithMessage("*cannot be null or empty*");
        }
    }
}