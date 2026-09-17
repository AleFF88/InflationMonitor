using InflationMonitor.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace InflationMonitor.Domain.Entities {
    /// <summary>
    /// Represents a historical exchange rate domain entity for a specific currency and monthly period, 
    /// enforcing core domain invariants and business rules.
    /// </summary>
    public class ExchangeRate {
        public int Id { get; private set; }
        public string CurrencyCode { get; private set; } = string.Empty;
        public DateOnly Date { get; private set; }
        public decimal Rate { get; private set; }

        // Required by Entity Framework Core to materialize objects from the database 
        //   without invoking domain validation
        private ExchangeRate() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExchangeRate"/> class, validating business rules, 
        /// normalizing the date to the 1st day of the month, and enforcing historical lower bounds.
        /// </summary>
        /// <param name="currencyCode">The currency code (e.g., USD, EUR).</param>
        /// <param name="date">The exchange rate record date (will be normalized to the first day of the month).</param>
        /// <param name="rate">The exchange rate value (must be greater than zero).</param>
        /// <exception cref="DomainArgumentOutOfRangeException">Thrown when currencyCode is null/empty or rate is less than or equal to zero.</exception>
        /// <exception cref="InvalidHistoricalPeriodException">Thrown when the date precedes the introduction of the Ukrainian Hryvnia (September 1996).</exception>
        [JsonConstructor]
        public ExchangeRate(string currencyCode, DateOnly date, decimal rate) {
            if (string.IsNullOrWhiteSpace(currencyCode)) {
                throw new DomainArgumentOutOfRangeException(nameof(currencyCode), "Currency code cannot be null or empty.");
            }

            // Normalize the date to the 1st day of the month to enforce monthly domain invariant 
            //   and guarantee uniform key formatting during direct entity instantiation or seeding
            var normalizedDate = new DateOnly(date.Year, date.Month, 1);

            // Global lower bound for Ukrainian monetary system (Hryvnia introduced in Sept 1996)
            if (normalizedDate < new DateOnly(1996, 9, 1)) {
                throw new InvalidHistoricalPeriodException("Historical financial data for the Ukrainian Hryvnia (UAH) is only available starting from September 1996 (monetary reform).");
            }

            if (rate <= 0) {
                throw new DomainArgumentOutOfRangeException(nameof(rate), "Exchange rate must be greater than zero.");
            }

            CurrencyCode = currencyCode.ToUpperInvariant();
            Date = normalizedDate;
            Rate = rate;
        }
    }
}