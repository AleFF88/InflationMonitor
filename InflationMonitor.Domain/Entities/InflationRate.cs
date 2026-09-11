using InflationMonitor.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace InflationMonitor.Domain.Entities {
    public class InflationRate {
        public int Id { get; private set; }
        public DateOnly Date { get; private set; }
        public decimal Value { get; private set; }

        // Required by Entity Framework Core to materialize objects from the database 
        //   without invoking domain validation
        private InflationRate() { }

        // Used for explicitly creating valid domain instances and for JSON deserialization 
        //   during data seeding
        [JsonConstructor]
        public InflationRate(DateOnly date, decimal value) {
            // Normalize the date to the 1st day of the month before validating bounds
            var normalizedDate = new DateOnly(date.Year, date.Month, 1);

            // Global lower bound for official Ukrainian inflation index recording
            //   (data available since from 2000)
            if (normalizedDate < new DateOnly(2000, 1, 1)) {
                throw new InvalidHistoricalPeriodException("Inflation rate data is available only starting from January 2000.");
            }

            if (value < 0) {
                throw new DomainArgumentOutOfRangeException(nameof(value), "Inflation rate cannot be negative.");
            }

            Date = normalizedDate;
            Value = value;
        }
    }
}
