using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Application.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InflationMonitor.Application.Queries.CalculateComparison {
    public class CalculateComparisonQueryHandler : IRequestHandler<CalculateComparisonQuery, CalculateComparisonResponseDto> {
        private readonly IApplicationDbContext _context;

        public CalculateComparisonQueryHandler(IApplicationDbContext context) {
            _context = context;
        }

        public async Task<CalculateComparisonResponseDto> Handle(CalculateComparisonQuery request, CancellationToken cancellationToken) {

            decimal? inflationEquivalent = await CalculateInflationEquivalentAsync(request.StartDate, request.EndDate, request.Amount, cancellationToken);

            var currencyCodes = new[] { "USD", "EUR" }; 
            var currencyEquivalents = await CalculateCurrenciesEquivalentsAsync(currencyCodes, request.StartDate, request.EndDate, request.Amount, cancellationToken); 

            return new CalculateComparisonResponseDto {
                StartDate = request.StartDate.ToString("yyyy-MM-dd"),
                EndDate = request.EndDate.ToString("yyyy-MM-dd"),
                InitialAmount = request.Amount,
                Summary = new CashUsdComparisonSummaryDto {
                    CashGrivna = request.Amount,
                    InflationEquivalent = inflationEquivalent,
                    UsdEquivalent = currencyEquivalents.GetValueOrDefault("USD"),
                    EurEquivalent = currencyEquivalents.GetValueOrDefault("EUR")
                }
            };
        }

        private async Task<decimal?> CalculateInflationEquivalentAsync(DateTime startDate, DateTime endDate, decimal amount, CancellationToken cancellationToken) {

            // Calculate expected months count in the requested range inclusive
            int expectedMonthsCount = ((endDate.Year - startDate.Year) * 12)
                + endDate.Month - startDate.Month + 1;
            // Get a list of inflation data for the period
            var inflationIndices = await _context.InflationRates
                .AsNoTracking()
                .Where(x => x.Year > startDate.Year ||
                           (x.Year == startDate.Year && x.Month >= startDate.Month))
                .Where(x => x.Year < endDate.Year ||
                           (x.Year == endDate.Year && x.Month <= endDate.Month))
                .ToListAsync(cancellationToken);

            // TODO: Consider enriching the response DTO with metadata or warnings 
            //   explaining why a calculation returned null (e.g., historical data for
            //   EUR is available only starting from 1999-01, but 1998-05 was requested).
            if (inflationIndices.Count != expectedMonthsCount) {
                return null;
            }

            decimal inflationMultiplier = 1.0m;
            foreach (var index in inflationIndices) {
                inflationMultiplier *= index.Value;
            }

            return Math.Round(amount * inflationMultiplier, 2);

        }

        private async Task<Dictionary<string, decimal?>> CalculateCurrenciesEquivalentsAsync(
            string[] currencyCodes,
            DateTime startDate,
            DateTime endDate,
            decimal amount,
            CancellationToken cancellationToken) {

            var rates = await _context.ExchangeRates
                .AsNoTracking()
                .Where(x => currencyCodes.Contains(x.CurrencyCode) &&
                           ((x.Year == startDate.Year && x.Month == startDate.Month) ||
                            (x.Year == endDate.Year && x.Month == endDate.Month)))
                .ToListAsync(cancellationToken);
            var ratesByCurrency = rates.GroupBy(x => x.CurrencyCode);

            var result = new Dictionary<string, decimal?>();
            foreach (var currency in ratesByCurrency) {
                var startRate = currency.FirstOrDefault(x => x.Year == startDate.Year && x.Month == startDate.Month);
                var endRate = currency.FirstOrDefault(x => x.Year == endDate.Year && x.Month == endDate.Month);

                if (startRate == null || endRate == null) {
                    result[currency.Key] = null;
                    continue;
                }

                decimal currencyBought = amount / startRate.Rate;
                result[currency.Key] = Math.Round(currencyBought * endRate.Rate, 2);
            }

            return result;
        }
    }
}
