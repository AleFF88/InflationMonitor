using InflationMonitor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InflationMonitor.Application.Strategies {
    public class BatchCurrencyStrategy : IBatchFinancialInstrumentStrategy {
        private readonly IApplicationDbContext _context;
        public string CategoryKey => "Currencies";

        public BatchCurrencyStrategy(IApplicationDbContext context) {
            _context = context;
        }

        public async Task<Dictionary<string, decimal?>> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateTime startDate, 
            DateTime endDate, 
            decimal amount, 
            CancellationToken cancellationToken) {

            var currencyCodesList = instrumentCodes.Select(c => c.ToUpperInvariant()).ToList();

            var rates = await _context.ExchangeRates
                .AsNoTracking()
                .Where(x => currencyCodesList.Contains(x.CurrencyCode) &&
                           ((x.Year == startDate.Year && x.Month == startDate.Month) ||
                            (x.Year == endDate.Year && x.Month == endDate.Month)))
                .ToListAsync(cancellationToken);
            var ratesByCurrency = rates.GroupBy(x => x.CurrencyCode);

            var result = new Dictionary<string, decimal?>();
            foreach (var currency in ratesByCurrency) {
                var startRate = currency.FirstOrDefault(x => x.Year == startDate.Year && x.Month == startDate.Month);
                var endRate = currency.FirstOrDefault(x => x.Year == endDate.Year && x.Month == endDate.Month);

                // TODO: Consider enriching the response DTO with metadata or warnings 
                //   explaining why a calculation returned null (e.g., historical data for
                //   EUR is available only starting from 1999-01, but 1998-05 was requested).
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
