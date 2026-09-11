using InflationMonitor.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace InflationMonitor.Domain.Entities {
    public class ExchangeRate {
        public int Id { get; private set; }
        public string CurrencyCode { get; private set; } = string.Empty;
        public DateOnly Date { get; private set; }
        public decimal Rate { get; private set; }

        // Required by Entity Framework Core to materialize objects from the database 
        //   without invoking domain validation
        private ExchangeRate() { }

        // Used for explicitly creating valid domain instances and for JSON deserialization 
        //   during data seeding
        [JsonConstructor]
        public ExchangeRate(string currencyCode, DateOnly date, decimal rate) {
            if (string.IsNullOrWhiteSpace(currencyCode)) {
                throw new DomainArgumentOutOfRangeException(nameof(currencyCode), "Currency code cannot be null or empty.");
            }

            // Normalize the date to the 1st day of the month before validating bounds
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
