using InflationMonitor.Application.Dtos;
using MediatR;

namespace InflationMonitor.Application.Queries.CalculateComparison {
    public class CalculateComparisonQuery : IRequest<CalculateComparisonResponseDto> {

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal Amount { get; set; }

        public CalculateComparisonQuery(DateOnly startDate, DateOnly endDate, decimal amount) {
            // Normalize dates to the 1st day of the month to guarantee exact key matching
            //   with database records
            StartDate = new DateOnly(startDate.Year, startDate.Month, 1);
            EndDate = new DateOnly(endDate.Year, endDate.Month, 1);
            Amount = amount;
        }
    }
}