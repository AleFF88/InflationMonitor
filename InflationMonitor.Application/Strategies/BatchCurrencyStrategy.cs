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
            DateTime startDate,
            DateTime endDate,
            decimal amount,
            CancellationToken cancellationToken) {

            var currencyCodesList = instrumentCodes.Select(c => c.ToUpperInvariant()).Distinct().ToList();

            // Form target keys (Code_Year_Month) for start and end dates
            var requiredKeysMap = new Dictionary<string, (string Code, int Year, int Month)>(); 
            foreach (var code in currencyCodesList) { 
                requiredKeysMap[$"{code}_{startDate.Year}_{startDate.Month}"] = (code, startDate.Year, startDate.Month); 
                requiredKeysMap[$"{code}_{endDate.Year}_{endDate.Month}"] = (code, endDate.Year, endDate.Month); 
            } 

            var fetchedRates = new Dictionary<string, ExchangeRate>(); 
            var missingKeys = new List<string>(); 

            // Retrieve available records from cache
            foreach (var keyPair in requiredKeysMap) { 
                var cacheKey = $"currency_{keyPair.Key}"; 
                if (_cache.TryGetValue(cacheKey, out ExchangeRate? cachedRate) && cachedRate != null) { 
                    fetchedRates[keyPair.Key] = cachedRate; 
                } else { 
                    missingKeys.Add(keyPair.Key); 
                } 
            } 

            // Fetch missing periods from database if cache miss occurred
            if (missingKeys.Count != 0) { 
                var fetchedFromDb = await _context.ExchangeRates 
                    .AsNoTracking() 
                    .Where(x => missingKeys.Contains(x.CurrencyCode + "_" + x.Year.ToString() + "_" + x.Month.ToString())) 
                    .ToListAsync(cancellationToken); 

                var cacheEntryOptions = new MemoryCacheEntryOptions() 
                    .SetSize(1) 
                    .SetAbsoluteExpiration(TimeSpan.FromDays(10)); 

                foreach (var rate in fetchedFromDb) { 
                    var itemDbKey = $"{rate.CurrencyCode}_{rate.Year}_{rate.Month}"; 
                    var cacheKey = $"currency_{itemDbKey}"; 
                    _cache.Set(cacheKey, rate, cacheEntryOptions); 
                    fetchedRates[itemDbKey] = rate; 
                } 
            } 

            var result = new Dictionary<string, decimal?>();

            foreach (var currencyCode in currencyCodesList) { 
                var startDbKey = $"{currencyCode}_{startDate.Year}_{startDate.Month}"; 
                var endDbKey = $"{currencyCode}_{endDate.Year}_{endDate.Month}"; 

                var hasStart = fetchedRates.TryGetValue(startDbKey, out var startRate); 
                var hasEnd = fetchedRates.TryGetValue(endDbKey, out var endRate); 

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