namespace InflationMonitor.Application.Common.Interfaces {
    public interface IBatchFinancialInstrumentStrategy {
        string CategoryKey { get; }

        Task<Dictionary<string, decimal?>> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateOnly startDate,
            DateOnly endDate, 
            decimal amount, 
            CancellationToken cancellationToken);
    }
}
