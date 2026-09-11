using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InflationMonitor.Application.Strategies {
    public class BatchCurrencyStrategy : IBatchFinancialInstrumentStrategy {
        private readonly IApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public string CategoryKey => "Currencies";

        public BatchCurrencyStrategy(IApplicationDbContext context, IMemoryCache cache) {
            _context = context;
            _cache = cache;
        }

        public async Task<Dictionary<string, decimal?>> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateOnly startDate,
            DateOnly endDate,
            decimal amount,
            CancellationToken cancellationToken) {

            var distinctCurrencyCodes = instrumentCodes.Select(c => c.ToUpperInvariant()).Distinct().ToList();

            // Normalize dates to the 1st day of the month to guarantee exact key matching
            //   with domain constructors and database records
            var normalizedStart = new DateOnly(startDate.Year, startDate.Month, 1);
            var normalizedEnd = new DateOnly(endDate.Year, endDate.Month, 1);

            // Check cache for both start and end dates of each requested currency 
            var fetchedRates = new Dictionary<string, ExchangeRate>();
            var missingRatesMap = new Dictionary<string, (string CurrencyCode, DateOnly Date)>();
            var datesToCheck = new[] { normalizedStart, normalizedEnd };
            foreach (var currencyCode in distinctCurrencyCodes) {
                foreach (var date in datesToCheck) {
                    var periodKey = $"{currencyCode}_{date:yyyy-MM-01}";
                    var cacheKey = $"currency_{periodKey}";

                    if (_cache.TryGetValue(cacheKey, out ExchangeRate? cachedRate) && cachedRate != null) {
                        fetchedRates[periodKey] = cachedRate;
                    } else {
                        missingRatesMap[periodKey] = (currencyCode, date);
                    }
                }
            }

            // Fetch missing rates from database if any entries were not found in cache
            if (missingRatesMap.Count != 0) {
                var targetCurrencyCodes = missingRatesMap.Values.Select(v => v.CurrencyCode).Distinct().ToList();
                var targetDates = missingRatesMap.Values.Select(v => v.Date).Distinct().ToList();

                var fetchedFromDb = await _context.ExchangeRates
                    .AsNoTracking()
                    .Where(x => targetCurrencyCodes.Contains(x.CurrencyCode)
                                && targetDates.Contains(x.Date))
                    .ToListAsync(cancellationToken);

                // Store rates not cached earlier to the memory cache and enrich local collection 
                //   to perform the calculations for the requested period 
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSize(1)
                    .SetAbsoluteExpiration(TimeSpan.FromDays(10));

                foreach (var rate in fetchedFromDb) {
                    var periodKey = $"{rate.CurrencyCode}_{rate.Date:yyyy-MM-01}";
                    var cacheKey = $"currency_{periodKey}";
                    _cache.Set(cacheKey, rate, cacheEntryOptions);
                    fetchedRates[periodKey] = rate;
                }
            }

            // Calculate final financial equivalents for each currency using collected rates
            var result = new Dictionary<string, decimal?>();
            foreach (var currencyCode in distinctCurrencyCodes) {
                var startPeriodKey = $"{currencyCode}_{normalizedStart:yyyy-MM-01}";
                var endPeriodKey = $"{currencyCode}_{normalizedEnd:yyyy-MM-01}";

                var hasStart = fetchedRates.TryGetValue(startPeriodKey, out var startRate);
                var hasEnd = fetchedRates.TryGetValue(endPeriodKey, out var endRate);

                // TODO: Consider enriching the response DTO with metadata or warnings 
                //   explaining why a calculation returned null (e.g., historical data for
                //   EUR is available only starting from 1999-01, but 1998-05 was requested).
                if (!hasStart || !hasEnd || startRate == null || endRate == null) {
                    result[currencyCode] = null;
                    continue;
                }

                decimal currencyBought = amount / startRate.Rate;
                result[currencyCode] = Math.Round(currencyBought * endRate.Rate, 2);
            }

            return result;
        }
    }
}