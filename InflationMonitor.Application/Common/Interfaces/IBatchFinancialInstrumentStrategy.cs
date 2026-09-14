namespace InflationMonitor.Application.Common.Interfaces {
    public readonly struct CalculationResult {
        public Dictionary<string, decimal?> Equivalents { get; }
        public List<string> Warnings { get; }

        public CalculationResult(Dictionary<string, decimal?> equivalents, List<string> warnings) {
            Equivalents = equivalents;
            Warnings = warnings;
        }
    }

    public interface IBatchFinancialInstrumentStrategy {
        string CategoryKey { get; }

        Task<CalculationResult> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateOnly startDate,
            DateOnly endDate,
            decimal amount,
            CancellationToken cancellationToken);
    }
}
