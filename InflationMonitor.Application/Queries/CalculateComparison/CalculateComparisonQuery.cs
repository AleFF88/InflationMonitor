using InflationMonitor.Application.Dtos;
using MediatR;

namespace InflationMonitor.Application.Queries.CalculateComparison {
    /// <summary>
    /// Represents a MediatR query for calculating and comparing financial purchasing power equivalents over a specified period.
    /// </summary>
    public class CalculateComparisonQuery : IRequest<CalculateComparisonResponseDto> {

        /// <summary>
        /// Gets the start date of the calculation period (normalized to the 1st day of the month).
        /// </summary>
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// Gets the end date of the calculation period (normalized to the 1st day of the month).
        /// </summary>
        public DateOnly EndDate { get; set; }

        /// <summary>
        /// Gets the initial monetary amount in Ukrainian Hryvnia (UAH).
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalculateComparisonQuery"/> class, 
        /// automatically normalizing the start and end dates to the 1st day of their respective months 
        /// to ensure exact key matching with database records.
        /// </summary>
        /// <param name="startDate">The raw start date provided by the caller.</param>
        /// <param name="endDate">The raw end date provided by the caller.</param>
        /// <param name="amount">The initial monetary amount.</param>
        public CalculateComparisonQuery(DateOnly startDate, DateOnly endDate, decimal amount) {
            // Normalize dates to the 1st day of the month to guarantee exact key matching
            //   with database records
            StartDate = new DateOnly(startDate.Year, startDate.Month, 1);
            EndDate = new DateOnly(endDate.Year, endDate.Month, 1);
            Amount = amount;
        }
    }
}