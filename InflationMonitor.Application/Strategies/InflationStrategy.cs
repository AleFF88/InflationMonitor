using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InflationMonitor.Application.Strategies {
    public class InflationStrategy : IBatchFinancialInstrumentStrategy {
        private readonly IApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public string CategoryKey => InflationConstants.CategoryKey;

        public InflationStrategy(IApplicationDbContext context, IMemoryCache cache) {
            _context = context;
            _cache = cache;
        }

        public async Task<CalculationResult> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateOnly startDate,
            DateOnly endDate,
            decimal amount,
            CancellationToken cancellationToken) {

            // Generate full list of required periods (normalized to the 1st day of each month)
            var requiredPeriods = GetRequiredPeriods(startDate, endDate);
            int expectedMonthsCount = requiredPeriods.Count;

            var fetchedRates = new List<InflationRate>();
            var missingPeriods = new List<DateOnly>();

            // Retrieve available records from cache
            foreach (var period in requiredPeriods) {
                var cacheKey = $"inflation_{period:yyyy-MM-01}";

                if (_cache.TryGetValue(cacheKey, out InflationRate? cachedRate) && cachedRate != null) {
                    fetchedRates.Add(cachedRate);
                } else {
                    missingPeriods.Add(period);
                }
            }

            // Fetch missing rates from database if any entries were not found in cache
            if (missingPeriods.Count != 0) {
                var fetchedFromDb = await _context.InflationRates
                    .AsNoTracking()
                    .Where(x => missingPeriods.Contains(x.Date))
                    .OrderBy(x => x.Date)
                    .ToListAsync(cancellationToken);

                // Store rates not cached earlier to the memory cache and enrich local collection 
                //   to perform the calculations for the requested period
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSize(1)
                    .SetAbsoluteExpiration(TimeSpan.FromDays(10));

                foreach (var rate in fetchedFromDb) {
                    var cacheKey = $"inflation_{rate.Date:yyyy-MM-01}";
                    _cache.Set(cacheKey, rate, cacheEntryOptions);
                    fetchedRates.Add(rate);
                }
            }

            var result = new Dictionary<string, decimal?>();
            var warnings = new List<string>();
            var minSupportedDate = InflationConstants.InflationMinSupportedDates[InflationConstants.Codes.Cpi];
            if (fetchedRates.Count != expectedMonthsCount) {
                var instrumentCode = InflationConstants.Codes.Cpi;
                result[instrumentCode] = null;
                var maxFetchedDate = fetchedRates.MaxBy(x => x.Date)?.Date;

                if (requiredPeriods[0] < minSupportedDate) {
                    warnings.Add($"Historical inflation data for '{instrumentCode}' is available only starting from {minSupportedDate:yyyy-MM}, but {requiredPeriods[0]:yyyy-MM} was requested.");
                } else if (maxFetchedDate.HasValue && endDate > maxFetchedDate.Value) { 
                    warnings.Add($"Historical inflation data for '{instrumentCode}' is available only up to {maxFetchedDate.Value:yyyy-MM}, but {endDate:yyyy-MM} was requested."); 
                } else {
                    warnings.Add($"Historical inflation data for '{instrumentCode}' is incomplete or unavailable for the requested period.");
                }
                return new CalculationResult(result, warnings);
            }

            decimal inflationMultiplier = 1.0m;
            foreach (var index in fetchedRates.OrderBy(x => x.Date)) {
                inflationMultiplier *= index.Rate;
            }

            result[InflationConstants.Codes.Cpi] = Math.Round(amount * inflationMultiplier, 2);
            return new CalculationResult(result, warnings);
        }

        private static List<DateOnly> GetRequiredPeriods(DateOnly startDate, DateOnly endDate) {
            var periods = new List<DateOnly>();
            var current = startDate;
            var last = endDate;

            while (current <= last) {
                periods.Add(current);
                current = current.AddMonths(1);
            }

            return periods;
        }
    }
}