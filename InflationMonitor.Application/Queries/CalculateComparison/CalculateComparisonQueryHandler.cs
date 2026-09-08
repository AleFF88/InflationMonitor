using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Application.Dtos;
using MediatR;

namespace InflationMonitor.Application.Queries.CalculateComparison {
    public class CalculateComparisonQueryHandler : IRequestHandler<CalculateComparisonQuery, CalculateComparisonResponseDto> {

        private readonly IFinancialInstrumentFactory _instrumentFactory;

        public CalculateComparisonQueryHandler(IFinancialInstrumentFactory instrumentFactory) {
            _instrumentFactory = instrumentFactory;
        }

        public async Task<CalculateComparisonResponseDto> Handle(CalculateComparisonQuery request, CancellationToken cancellationToken) {

            var inflationStrategy = _instrumentFactory.GetStrategy("Inflation");
            var inflationResults = await inflationStrategy.CalculateEquivalentsAsync(
                [], 
                request.StartDate,
                request.EndDate, 
                request.Amount, 
                cancellationToken);

            var currencyStrategy = _instrumentFactory.GetStrategy("Currencies");
            var currencyResults = await currencyStrategy.CalculateEquivalentsAsync(
                ["USD", "EUR"], 
                request.StartDate, 
                request.EndDate, 
                request.Amount, 
                cancellationToken);

            return new CalculateComparisonResponseDto {
                StartDate = request.StartDate.ToString("yyyy-MM-dd"),
                EndDate = request.EndDate.ToString("yyyy-MM-dd"),
                InitialAmount = request.Amount,
                Summary = new FinancialComparisonSummaryDto {
                    CashGrivna = request.Amount,
                    InflationEquivalent = inflationResults.GetValueOrDefault("Inflation"),
                    UsdEquivalent = currencyResults.GetValueOrDefault("USD"),
                    EurEquivalent = currencyResults.GetValueOrDefault("EUR")
                }
            };
        }
    }
}
