using InflationMonitor.Domain.Exceptions;

namespace InflationMonitor.Domain.Entities {
    public class InflationRate {
        public int Id { get; private set; }
        public int Year { get; private set; }
        public int Month { get; private set; }
        public decimal Value { get; private set; }

        private InflationRate() { }

        public InflationRate(int year, int month, decimal value) {
            if (year < 2000) {
                throw new InvalidHistoricalPeriodException("Inflation rate has only been available since 2000.");
            }

            if (month < 1 || month > 12) {
                throw new DomainArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
            }

            if (value < 0) {
                throw new DomainArgumentOutOfRangeException(nameof(value), "Inflation rate cannot be negative.");
            }
                Year = year;
                Month = month;
                Value = value;
        }
    }
}
