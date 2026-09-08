using InflationMonitor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InflationMonitor.Application.Strategies {
    public class InflationStrategy : IBatchFinancialInstrumentStrategy {
        private readonly IApplicationDbContext _context;
        public string CategoryKey => "Inflation";

        public InflationStrategy(IApplicationDbContext context) {
            _context = context;
        }

        public async Task<Dictionary<string, decimal?>> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateTime startDate,
            DateTime endDate,
            decimal amount,
            CancellationToken cancellationToken) {

            // Calculate expected months count in the requested range inclusive
            int expectedMonthsCount = ((endDate.Year - startDate.Year) * 12)
                + endDate.Month - startDate.Month + 1;
            // Get a list of inflation data for the period
            var inflationIndices = await _context.InflationRates
                .AsNoTracking()
                .Where(x => x.Year > startDate.Year ||
                           (x.Year == startDate.Year && x.Month >= startDate.Month))
                .Where(x => x.Year < endDate.Year ||
                           (x.Year == endDate.Year && x.Month <= endDate.Month))
                .ToListAsync(cancellationToken);

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
                inflationMultiplier *= index.Value;
            }

            result[CategoryKey] = Math.Round(amount * inflationMultiplier, 2);
            return result;
        }
    }
}
