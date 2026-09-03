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

            decimal? inflationEquivalent = await CalculateInflationEquivalentAsync(request, cancellationToken);

            decimal ? usdEquivalent = await CalculateCurrencyEquivalentAsync("USD", request.StartDate, request.EndDate, request.Amount, cancellationToken);

            decimal? eurEquivalent = await CalculateCurrencyEquivalentAsync("EUR", request.StartDate, request.EndDate, request.Amount, cancellationToken);

            return new CalculateComparisonResponseDto {
                StartDate = request.StartDate.ToString("yyyy-MM-dd"),
                EndDate = request.EndDate.ToString("yyyy-MM-dd"),
                InitialAmount = request.Amount,
                Summary = new CashUsdComparisonSummaryDto {
                    CashGrivna = request.Amount,
                    InflationEquivalent = inflationEquivalent,
                    UsdEquivalent = usdEquivalent,
                    EurEquivalent = eurEquivalent
                }
            };
        }

        private async Task<decimal?> CalculateInflationEquivalentAsync(CalculateComparisonQuery request, CancellationToken cancellationToken) {

            // Calculate expected months count in the requested range inclusive
            int expectedMonthsCount = ((request.EndDate.Year - request.StartDate.Year) * 12)
                + request.EndDate.Month - request.StartDate.Month + 1;

            // Get a list of inflation data for the period
            var inflationIndices = await _context.InflationRates
                           .Where(x => x.Year > request.StartDate.Year ||
                           (x.Year == request.StartDate.Year && x.Month >= request.StartDate.Month))
                .Where(x => x.Year < request.EndDate.Year ||
                           (x.Year == request.EndDate.Year && x.Month <= request.EndDate.Month))
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

            return Math.Round(request.Amount * inflationMultiplier, 2);
            
        }

        private async Task<decimal?> CalculateCurrencyEquivalentAsync(
            string currencyCode,
            DateTime startDate,
            DateTime endDate,
            decimal amount,
            CancellationToken cancellationToken) {

            var startRate = await _context.ExchangeRates
                .FirstOrDefaultAsync(x => x.CurrencyCode == currencyCode &&
                                          x.Year == startDate.Year &&
                                          x.Month == startDate.Month,
                                     cancellationToken);

            var endRate = await _context.ExchangeRates
                .FirstOrDefaultAsync(x => x.CurrencyCode == currencyCode &&
                                          x.Year == endDate.Year &&
                                          x.Month == endDate.Month,
                                     cancellationToken);

            // TODO: Consider enriching the response DTO with metadata or warnings 
            //   explaining why a calculation returned null (e.g., historical data for
            //   EUR is available only starting from 1999-01, but 1998-05 was requested).
            if (startRate == null || endRate == null) {
                return null;
            }

            decimal currencyBought = amount / startRate.Rate;
            return Math.Round(currencyBought * endRate.Rate, 2);
        }
    }
}
