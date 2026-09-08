namespace InflationMonitor.Application.Common.Interfaces {
    public interface IBatchFinancialInstrumentStrategy {
        string CategoryKey { get; }

        Task<Dictionary<string, decimal?>> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateTime startDate, 
            DateTime endDate, 
            decimal amount, 
            CancellationToken cancellationToken);
    }
}
