using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Application.Dtos;
using InflationMonitor.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InflationMonitor.Application.Queries.CalculateComparison {
    public class CalculateComparisonQueryHandler : IRequestHandler<CalculateComparisonQuery, CalculateComparisonResponseDto> {
        private readonly IApplicationDbContext _context; 

        public CalculateComparisonQueryHandler(IApplicationDbContext context) {
            _context = context;
        }

        public async Task<CalculateComparisonResponseDto> Handle(CalculateComparisonQuery request, CancellationToken cancellationToken) {
            // Fetch monthly inflation indices for the requested timeframe from the database
            var inflationIndices = await _context.InflationRates
                .Where(x => x.Year > request.StartDate.Year ||
                           (x.Year == request.StartDate.Year && x.Month >= request.StartDate.Month))
                .Where(x => x.Year < request.EndDate.Year ||
                           (x.Year == request.EndDate.Year && x.Month <= request.EndDate.Month))
                .ToListAsync(cancellationToken);

            // Throw exception if no inflation data exists for the selected period
            if (inflationIndices.Count == 0) {
                throw new HistoricalDataNotFoundException($"No inflation indices found for period {request.StartDate:yyyy-MM} - {request.EndDate:yyyy-MM}"); 
            }
            // TODO Handle case where some months (not all) are missing data

            // Calculate cumulative inflation by multiplying monthly rate multipliers
            decimal inflationMultiplier = 1.0m; 
            foreach (var index in inflationIndices) {
                inflationMultiplier *= index.Value; 
            }

            // Calculate amount equivalent adjusted for inflation
            decimal inflationEquivalent = request.Amount * inflationMultiplier; // Final inflated value

            // Fetch USD exchange rate for the start date or closest preceding month
            var startRate = await _context.ExchangeRates
                .Where(x => x.CurrencyCode == "USD" &&
                           (x.Year < request.StartDate.Year ||
                           (x.Year == request.StartDate.Year && x.Month <= request.StartDate.Month)))
                .OrderByDescending(x => x.Year) // Sort by year descending to get latest available rate
                .ThenByDescending(x => x.Month) // Sort by month descending
                .FirstOrDefaultAsync(cancellationToken); // Get closest matching rate

            // Throw exception if starting exchange rate is missing
            if (startRate == null) {
                throw new HistoricalDataNotFoundException($"USD exchange rate is missing for start date: {request.StartDate:yyyy-MM}"); // Handle missing initial rate
            }

            // Fetch USD exchange rate for the end date or closest preceding month
            var endRate = await _context.ExchangeRates
                .Where(x => x.CurrencyCode == "USD" &&
                           (x.Year < request.EndDate.Year ||
                           (x.Year == request.EndDate.Year && x.Month <= request.EndDate.Month)))
                .OrderByDescending(x => x.Year) // Sort by year descending
                .ThenByDescending(x => x.Month) // Sort by month descending
                .FirstOrDefaultAsync(cancellationToken); // Get closest matching rate

            // Throw exception if ending exchange rate is missing
            if (endRate == null) {
                throw new HistoricalDataNotFoundException($"USD exchange rate is missing for end date: {request.EndDate:yyyy-MM}"); // Handle missing final rate
            }
            // TODO Handle case where some months (not all) are missing data

            // Calculate purchasing power value via initial USD buy and final conversion
            decimal usdBought = request.Amount / startRate.Rate; // Convert initial amount to USD
            decimal usdEquivalent = usdBought * endRate.Rate; // Convert USD back to national currency at end rate

            // Build and return final response DTO
            return new CalculateComparisonResponseDto {
                StartDate = request.StartDate.ToString("yyyy-MM-dd"), 
                EndDate = request.EndDate.ToString("yyyy-MM-dd"),     
                InitialAmount = request.Amount, 
                Summary = new CashUsdComparisonSummaryDto {
                    CashGrivna = request.Amount, 
                    InflationEquivalent = Math.Round(inflationEquivalent, 2),
                    UsdEquivalent = Math.Round(usdEquivalent, 2) 
                }
            };
        }
    }
}