using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InflationMonitor.Application.Strategies {
    /// <summary>
    /// Implements a batch processing strategy for calculating inflation-adjusted equivalents 
    /// over a specified period.
    /// </summary>
    public class InflationStrategy : IBatchFinancialInstrumentStrategy {
        private readonly IApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public string CategoryKey => InflationConstants.CategoryKey;

        public InflationStrategy(IApplicationDbContext context, IMemoryCache cache) {
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

            // Generate full list of required periods normalized to the 1st day of each month
            var requiredPeriods = GetRequiredPeriods(startDate, endDate);
            int expectedMonthsCount = requiredPeriods.Count;

            var fetchedRates = new List<InflationRate>();
            var missingPeriods = new List<DateOnly>();

            // Collect rates available in memory cache and track missing periods
            foreach (var period in requiredPeriods) {
                var cacheKey = $"inflation_{BuildPeriodKey(period)}";

                if (_cache.TryGetValue(cacheKey, out InflationRate? cachedRate) && cachedRate != null) {
                    fetchedRates.Add(cachedRate);
                } else {
                    missingPeriods.Add(period);
                }
            }

            // Fetch missing rates from database in a single query if any entries were missing in cache
            if (missingPeriods.Count != 0) {
                var fetchedFromDb = await _context.InflationRates
                    .AsNoTracking()
                    .Where(x => missingPeriods.Contains(x.Date))
                    .OrderBy(x => x.Date)
                    .ToListAsync(cancellationToken);

                CacheAndStoreRates(fetchedFromDb, fetchedRates);
            }

            var result = new Dictionary<string, decimal?>();
            var warnings = new List<string>();

            // Validate completeness of fetched rates for requested range
            if (fetchedRates.Count != expectedMonthsCount) {
                var instrumentCode = InflationConstants.Codes.Cpi;
                result[instrumentCode] = null;

                var warning = BuildWarningForMissingRates(requiredPeriods[0], endDate, fetchedRates);
                warnings.Add(warning);

                return new CalculationResult(result, warnings);
            }

            // Calculate compound inflation multiplier across the entire period
            decimal inflationMultiplier = 1.0m;
            foreach (var index in fetchedRates.OrderBy(x => x.Date)) {
                inflationMultiplier *= index.Rate;
            }

            result[InflationConstants.Codes.Cpi] = Math.Round(amount * inflationMultiplier, 2);
            return new CalculationResult(result, warnings);
        }

        /// <summary>
        /// Evaluates period boundaries for missing inflation records and produces a descriptive warning message.
        /// </summary>
        /// <param name="requestedStartDate">The start date requested by the user.</param>
        /// <param name="requestedEndDate">The end date requested by the user.</param>
        /// <param name="fetchedRates">Collection of successfully retrieved inflation rate records.</param>
        /// <returns>A formatted warning string detailing the historical data limitation.</returns>
        private static string BuildWarningForMissingRates(
            DateOnly requestedStartDate,
            DateOnly requestedEndDate,
            IReadOnlyCollection<InflationRate> fetchedRates) {

            var instrumentCode = InflationConstants.Codes.Cpi;
            var minSupportedDate = InflationConstants.InflationMinSupportedDates[instrumentCode];
            var maxFetchedDate = fetchedRates.MaxBy(x => x.Date)?.Date;

            if (requestedStartDate < minSupportedDate) {
                return $"Historical inflation data for '{instrumentCode}' is available only starting from {minSupportedDate:yyyy-MM}, but {requestedStartDate:yyyy-MM} was requested.";
            }

            if (maxFetchedDate.HasValue && requestedEndDate > maxFetchedDate.Value) {
                return $"Historical inflation data for '{instrumentCode}' is available only up to {maxFetchedDate.Value:yyyy-MM}, but {requestedEndDate:yyyy-MM} was requested.";
            }

            return $"Historical inflation data for '{instrumentCode}' is incomplete or unavailable for the requested period.";
        }

        /// <summary>
        /// Caches newly fetched inflation rates in memory and updates the local rates collection.
        /// </summary>
        /// <param name="rates">Collection of inflation rate entities fetched from the database.</param>
        /// <param name="fetchedRates">Target local collection to update with cached rates.</param>
        private void CacheAndStoreRates(IEnumerable<InflationRate> rates, List<InflationRate> fetchedRates) {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(1)
                .SetAbsoluteExpiration(TimeSpan.FromDays(10));

            foreach (var rate in rates) {
                var cacheKey = $"inflation_{BuildPeriodKey(rate.Date)}";
                _cache.Set(cacheKey, rate, cacheEntryOptions);
                fetchedRates.Add(rate);
            }
        }

        /// <summary>
        /// Formats a period date into a standardized period key string (yyyy-MM-01).
        /// </summary>
        /// <param name="date">The date to format.</param>
        /// <returns>A string representation of the date formatted as yyyy-MM-01.</returns>
        private static string BuildPeriodKey(DateOnly date) =>
            $"{date:yyyy-MM-01}";

        /// <summary>
        /// Generates a sequential list of monthly dates between start and end boundaries.
        /// </summary>
        /// <param name="startDate">Start boundary date.</param>
        /// <param name="endDate">End boundary date.</param>
        /// <returns>A list containing monthly DateOnly instances covering the requested range.</returns>
        private static List<DateOnly> GetRequiredPeriods(DateOnly startDate, DateOnly endDate) {
            var periods = new List<DateOnly>();
            var current = startDate;

            while (current <= endDate) {
                periods.Add(current);
                current = current.AddMonths(1);
            }

            return periods;
        }
    }
}