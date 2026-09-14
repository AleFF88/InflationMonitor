using InflationMonitor.Application.Common.Constants;
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

            var inflationStrategy = _instrumentFactory.GetStrategy(FinancialInstrumentCategories.Inflation);
            var inflationResult = await inflationStrategy.CalculateEquivalentsAsync(
                [], 
                request.StartDate,
                request.EndDate, 
                request.Amount, 
                cancellationToken);

            var currencyStrategy = _instrumentFactory.GetStrategy(FinancialInstrumentCategories.Currencies);
            var currencyResult = await currencyStrategy.CalculateEquivalentsAsync(
                [CurrencyCodes.Usd, CurrencyCodes.Eur], 
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
                    InflationEquivalent = inflationResult.Equivalents.GetValueOrDefault(FinancialInstrumentCategories.Inflation),
                    UsdEquivalent = currencyResult.Equivalents.GetValueOrDefault(CurrencyCodes.Usd),
                    EurEquivalent = currencyResult.Equivalents.GetValueOrDefault(CurrencyCodes.Eur)
                },
                Warnings = inflationResult.Warnings.Concat(currencyResult.Warnings).ToList()   
            };
        }
    }
}
