using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InflationMonitor.Application.Strategies {
    /// <summary>
    /// Implements a batch processing strategy for calculating currency exchange rate equivalents
    /// over a specified period.
    /// </summary>
    public class BatchCurrencyStrategy : IBatchFinancialInstrumentStrategy {
        private readonly IApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public string CategoryKey => CurrencyConstants.CategoryKey;

        public BatchCurrencyStrategy(IApplicationDbContext context, IMemoryCache cache) {
            _context = context;
            _cache = cache;
        }

        /// <inheritdoc />
        public async Task<CalculationResult> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateOnly startDate,
            DateOnly endDate,
            decimal amount,
            CancellationToken cancellationToken) {

            var distinctCurrencyCodes = instrumentCodes.Select(c => c.ToUpperInvariant()).Distinct().ToList();

            // Collect rates available in memory cache and track missing rates
            var fetchedRates = new Dictionary<string, ExchangeRate>();
            var missingRatesMap = new Dictionary<string, (string CurrencyCode, DateOnly Date)>();
            var datesToCheck = new[] { startDate, endDate };
            foreach (var currencyCode in distinctCurrencyCodes) {
                foreach (var date in datesToCheck) {
                    var periodKey = BuildPeriodKey(currencyCode, date);
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

                CacheAndStoreRates(fetchedFromDb, fetchedRates);
            }

            // Calculate final financial equivalents for each currency using collected rates
            var result = new Dictionary<string, decimal?>();
            var warnings = new List<string>();
            foreach (var currencyCode in distinctCurrencyCodes) {
                var startPeriodKey = BuildPeriodKey(currencyCode, startDate); 
                var endPeriodKey = BuildPeriodKey(currencyCode, endDate); 

                var hasStart = fetchedRates.TryGetValue(startPeriodKey, out var startRate);
                var hasEnd = fetchedRates.TryGetValue(endPeriodKey, out var endRate);

                if (!hasStart || !hasEnd || startRate == null || endRate == null) {
                    result[currencyCode] = null;
                    var warning = await BuildWarningForMissingRateAsync(currencyCode, startDate, endDate, cancellationToken);
                    warnings.Add(warning);
                    continue;
                }

                decimal currencyBought = amount / startRate.Rate;
                result[currencyCode] = Math.Round(currencyBought * endRate.Rate, 2);
            }

            return new CalculationResult(result, warnings);
        }

        /// <summary>
        /// Caches fetched exchange rates in memory and updates the local rates collection dictionary.
        /// </summary>
        /// <param name="rates">Collection of exchange rate entities retrieved from the database.</param>
        /// <param name="fetchedRates">Dictionary of fetched rates to be updated.</param>
        private void CacheAndStoreRates(IEnumerable<ExchangeRate> rates, Dictionary<string, ExchangeRate> fetchedRates) { 
            var cacheEntryOptions = new MemoryCacheEntryOptions() 
                .SetSize(1) 
                .SetAbsoluteExpiration(TimeSpan.FromDays(10)); 

            foreach (var rate in rates) { 
                var periodKey = BuildPeriodKey(rate.CurrencyCode, rate.Date); 
                var cacheKey = $"currency_{periodKey}"; 
                _cache.Set(cacheKey, rate, cacheEntryOptions); 
                fetchedRates[periodKey] = rate; 
            } 
        }

        /// <summary>
        /// Generates a standardized composite period key for a given currency code and date.
        /// </summary>
        /// <param name="currencyCode">The currency code.</param>
        /// <param name="date">The date instance.</param>
        /// <returns>A composite string key in format {CurrencyCode}_{yyyy-MM-01}.</returns>
        private static string BuildPeriodKey(string currencyCode, DateOnly date) => 
            $"{currencyCode}_{date:yyyy-MM-01}";

        /// <summary> 
        /// Evaluates date boundaries for missing rates and constructs a user-friendly warning message, querying the maximum available currency date if necessary.  
        /// </summary> 
        /// <param name="currencyCode">The currency code being evaluated.</param>
        /// <param name="startDate">The requested start date.</param>
        /// <param name="endDate">The requested end date.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task containing the generated warning message string.</returns>
        private async Task<string> BuildWarningForMissingRateAsync(
            string currencyCode,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken) {
            if (CurrencyConstants.CurrencyMinSupportedDates.TryGetValue(currencyCode, out var minSupportedDate) && startDate < minSupportedDate) {
                return $"Historical exchange rate data for '{currencyCode}' is available only starting from {minSupportedDate:yyyy-MM}, but {startDate:yyyy-MM} was requested.";
            }

            var maxCurrencyDate = await GetMaxAvailableCurrencyDateAsync(currencyCode, cancellationToken);

            if (maxCurrencyDate.HasValue && endDate > maxCurrencyDate.Value) {
                return $"Historical exchange rate data for '{currencyCode}' is available only up to {maxCurrencyDate.Value:yyyy-MM}, but {endDate:yyyy-MM} was requested.";
            }

            return $"Historical exchange rate data for '{currencyCode}' is missing or incomplete for the requested period.";
        }

        /// <summary> 
        /// Retrieves the latest available rate date for a given currency from the database, caching the result in memory. 
        /// </summary> 
        /// <param name="currencyCode">The currency code to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task containing the maximum available DateOnly if present, otherwise null.</returns>
        private async Task<DateOnly?> GetMaxAvailableCurrencyDateAsync(string currencyCode, CancellationToken cancellationToken) {
            var cacheKey = $"currency_max_date_{currencyCode.ToLowerInvariant()}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry => {
                entry.SetSize(1);
                entry.SetAbsoluteExpiration(TimeSpan.FromHours(12));
                return await _context.ExchangeRates
                    .AsNoTracking()
                    .Where(x => x.CurrencyCode == currencyCode)
                    .MaxAsync(x => (DateOnly?)x.Date, cancellationToken);
            });
        }
    }
}