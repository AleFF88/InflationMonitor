using InflationMonitor.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace InflationMonitor.Domain.Entities {
    public class ExchangeRate {
        public int Id { get; private set; }
        public string CurrencyCode { get; private set; } = string.Empty;
        public int Year { get; private set; }
        public int Month { get; private set; }
        public decimal Rate { get; private set; }

        // Required by Entity Framework Core to materialize objects from the database 
        //   without invoking domain validation
        private ExchangeRate() { }

        // Used for explicitly creating valid domain instances and for JSON deserialization 
        //   during data seeding
        [JsonConstructor]
        public ExchangeRate(string currencyCode, int year, int month, decimal rate) {
            if (string.IsNullOrWhiteSpace(currencyCode)) {
                throw new DomainArgumentOutOfRangeException(nameof(currencyCode), "Currency code cannot be null or empty.");
            }

            if (year < 1996 || (year == 1996 && month < 9)) {
                throw new InvalidHistoricalPeriodException("Exchange rate has only been available since September 1996.");
            }

            if (month < 1 || month > 12) {
                throw new DomainArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
            }

            if (rate < 0) {
                throw new DomainArgumentOutOfRangeException(nameof(rate), "Exchange rate cannot be negative.");
            }

            CurrencyCode = currencyCode.ToUpperInvariant();
            Year = year;
            Month = month;
            Rate = rate;
        }
    }
}
