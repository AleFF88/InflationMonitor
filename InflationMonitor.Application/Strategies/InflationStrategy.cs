using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InflationMonitor.Application.Strategies {
    public class InflationStrategy : IBatchFinancialInstrumentStrategy {
        private readonly IApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public string CategoryKey => "Inflation";

        public InflationStrategy(IApplicationDbContext context, IMemoryCache cache) {
            _context = context;
            _cache = cache;
        }

        public async Task<Dictionary<string, decimal?>> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateTime startDate,
            DateTime endDate,
            decimal amount,
            CancellationToken cancellationToken) {


            // Generate full list of required periods (Year, Month) 		
            var requiredPeriods = GetRequiredPeriods(startDate, endDate);
            int expectedMonthsCount = requiredPeriods.Count;


            var inflationIndices = new List<InflationRate>();
            var missingPeriods = new List<(int Year, int Month)>();


            // Retrieve available records from cache 
            foreach (var period in requiredPeriods) {
                var cacheKey = $"inflation_rate_{period.Year}_{period.Month}";
                if (_cache.TryGetValue(cacheKey, out InflationRate? cachedRate) && cachedRate != null) {
                    inflationIndices.Add(cachedRate); 
                } else {
                    missingPeriods.Add(period);
                }
            }

            // Fetch missing periods from database if cache miss occurred 
            if (missingPeriods.Count != 0) {
                // Format required keys into string representations (e.g., "2023_1")
                var missingKeys = missingPeriods
                    .Select(p => $"{p.Year}_{p.Month}")
                    .ToList();

                // Query DB using formatted string keys translated directly to SQL
                var fetchedFromDb = await _context.InflationRates
                    .AsNoTracking()
                    .Where(x => missingKeys.Contains(x.Year.ToString() + "_" + x.Month.ToString())) 
                    .ToListAsync(cancellationToken);

                // Configure cache entry options with explicit size and expiration 
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSize(1)
                    .SetAbsoluteExpiration(TimeSpan.FromDays(10));

                // Save fetched database records to cache and append to final list 
                foreach (var rate in fetchedFromDb) { 
                    var cacheKey = $"inflation_rate_{rate.Year}_{rate.Month}";
                    _cache.Set(cacheKey, rate, cacheEntryOptions);
                    inflationIndices.Add(rate);
                }
            } 

            var result = new Dictionary<string, decimal?>();

            // TODO: Consider enriching the response DTO with metadata or warnings 
            //   explaining why a calculation returned null (e.g., historical data for
            //   EUR is available only starting from 1999-01, but 1998-05 was requested).
            if (inflationIndices.Count != expectedMonthsCount) {
                result[CategoryKey] = null;
                return result;
            }

            decimal inflationMultiplier = 1.0m;
            foreach (var index in inflationIndices) {
                inflationMultiplier *= index.Rate;
            }

            result[CategoryKey] = Math.Round(amount * inflationMultiplier, 2);
            return result;
        }

        private static List<(int Year, int Month)> GetRequiredPeriods(DateTime startDate, DateTime endDate) {

            var periods = new List<(int Year, int Month)>();
            var current = new DateTime(startDate.Year, startDate.Month, 1);
            var last = new DateTime(endDate.Year, endDate.Month, 1);

            while (current <= last) {
                periods.Add((current.Year, current.Month));
                current = current.AddMonths(1);
            }

            return periods;
        }
    }
}
