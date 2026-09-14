using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InflationMonitor.Application.Strategies {
    public class BatchCurrencyStrategy : IBatchFinancialInstrumentStrategy {
        private readonly IApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public string CategoryKey => CurrencyConstants.CategoryKey;

        public BatchCurrencyStrategy(IApplicationDbContext context, IMemoryCache cache) {
            _context = context;
            _cache = cache;
        }

        public async Task<CalculationResult> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateOnly startDate,
            DateOnly endDate,
            decimal amount,
            CancellationToken cancellationToken) {

            var distinctCurrencyCodes = instrumentCodes.Select(c => c.ToUpperInvariant()).Distinct().ToList();

            // Check cache for both start and end dates of each requested currency 
            var fetchedRates = new Dictionary<string, ExchangeRate>();
            var missingRatesMap = new Dictionary<string, (string CurrencyCode, DateOnly Date)>();
            var datesToCheck = new[] { startDate, endDate };
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
            var warnings = new List<string>();
            foreach (var currencyCode in distinctCurrencyCodes) {
                var startPeriodKey = $"{currencyCode}_{startDate:yyyy-MM-01}";
                var endPeriodKey = $"{currencyCode}_{endDate:yyyy-MM-01}";

                var hasStart = fetchedRates.TryGetValue(startPeriodKey, out var startRate);
                var hasEnd = fetchedRates.TryGetValue(endPeriodKey, out var endRate);

                if (!hasStart || !hasEnd || startRate == null || endRate == null) {
                    result[currencyCode] = null;

                    if (CurrencyConstants.CurrencyMinSupportedDates.TryGetValue(currencyCode, out var minSupportedDate) && startDate < minSupportedDate) {
                        warnings.Add($"Historical exchange rate data for '{currencyCode}' is available only starting from {minSupportedDate:yyyy-MM}, but {startDate:yyyy-MM} was requested.");
                    } else {
                        // Reuse IMemoryCache to get or store the latest available date for this currency
                        var cacheKey = $"currency_max_date_{currencyCode.ToLowerInvariant()}";
                        var maxCurrencyDate = await _cache.GetOrCreateAsync(cacheKey, async entry => { 
                            entry.SetSize(1); 
                            entry.SetAbsoluteExpiration(TimeSpan.FromHours(12)); 
                            return await _context.ExchangeRates 
                                .AsNoTracking() 
                                .Where(x => x.CurrencyCode == currencyCode) 
                                .MaxAsync(x => (DateOnly?)x.Date, cancellationToken); 
                        });

                        if (maxCurrencyDate.HasValue && endDate > maxCurrencyDate.Value) {
                            warnings.Add($"Historical exchange rate data for '{currencyCode}' is available only up to {maxCurrencyDate.Value:yyyy-MM}, but {endDate:yyyy-MM} was requested.");
                        } else {
                            warnings.Add($"Historical exchange rate data for '{currencyCode}' is missing or incomplete for the requested period.");
                        }

                    }

                    continue;
                }

                decimal currencyBought = amount / startRate.Rate;
                result[currencyCode] = Math.Round(currencyBought * endRate.Rate, 2);
            }

            return new CalculationResult(result, warnings);
        }
    }
}