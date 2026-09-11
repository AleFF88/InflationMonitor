using InflationMonitor.Application.Dtos;
using MediatR;

namespace InflationMonitor.Application.Queries.CalculateComparison {
    public class CalculateComparisonQuery : IRequest<CalculateComparisonResponseDto> {

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal Amount { get; set; }

        public CalculateComparisonQuery(DateOnly startDate, DateOnly endDate, decimal amount) {
            StartDate = startDate;
            EndDate = endDate;
            Amount = amount;
        }
    }
}