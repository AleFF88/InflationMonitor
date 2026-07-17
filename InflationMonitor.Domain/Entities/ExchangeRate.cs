using InflationMonitor.Domain.Exceptions;

namespace InflationMonitor.Domain.Entities {
    public class ExchangeRate {
        public int Id { get; private set; }
        public string CurrencyCode { get; private set; } = string.Empty;
        public int Year { get; private set; }
        public int Month { get; private set; }
        public decimal Rate { get; private set; }

        private ExchangeRate() { }

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
