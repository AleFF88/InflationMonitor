using InflationMonitor.Application.Dtos;
using MediatR;

namespace InflationMonitor.Application.Queries.CalculateComparison {
    public class CalculateComparisonQuery : IRequest<CalculateComparisonResponseDto> {

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }

        public CalculateComparisonQuery(DateTime startDate, DateTime endDate, decimal amount) {
            StartDate = startDate;
            EndDate = endDate;
            Amount = amount;
        }
    }
}