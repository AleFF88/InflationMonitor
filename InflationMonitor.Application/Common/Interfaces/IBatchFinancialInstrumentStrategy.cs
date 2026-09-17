namespace InflationMonitor.Application.Common.Interfaces {
    /// <summary>
    /// Represents the result of a batch financial equivalent calculation, containing computed equivalents and any processing warnings.
    /// </summary>
    public readonly struct CalculationResult {
        /// <summary>
        /// Gets the dictionary of calculated financial equivalents keyed by instrument code.
        /// </summary>
        public Dictionary<string, decimal?> Equivalents { get; }

        /// <summary>
        /// Gets the list of warning messages generated during the calculation process.
        /// </summary>
        public List<string> Warnings { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalculationResult"/> struct.
        /// </summary>
        /// <param name="equivalents">Dictionary of calculated equivalents.</param>
        /// <param name="warnings">List of generated warnings.</param>
        public CalculationResult(Dictionary<string, decimal?> equivalents, List<string> warnings) {
            Equivalents = equivalents;
            Warnings = warnings;
        }
    }

    /// <summary>
    /// Defines a strategy for calculating batch financial equivalents across specific instrument categories.
    /// </summary>
    public interface IBatchFinancialInstrumentStrategy {
        /// <summary>
        /// Gets the unique category key identifying the financial instrument strategy.
        /// </summary>
        string CategoryKey { get; }

        /// <summary>
        /// Asynchronously calculates financial equivalents for a specified batch of instrument codes between start and end dates.
        /// </summary>
        /// <param name="instrumentCodes">Collection of financial instrument or currency codes to process.</param>
        /// <param name="startDate">Start date of the calculation period.</param>
        /// <param name="endDate">End date of the calculation period.</param>
        /// <param name="amount">Initial monetary amount in Ukrainian Hryvnia (UAH).</param>
        /// <param name="cancellationToken">Cancellation token to propagate notification that operations should be canceled.</param>
        /// <returns>A task that represents the asynchronous calculation operation, containing the result with computed equivalents and warnings.</returns>
        Task<CalculationResult> CalculateEquivalentsAsync(
            IEnumerable<string> instrumentCodes,
            DateOnly startDate,
            DateOnly endDate,
            decimal amount,
            CancellationToken cancellationToken);
    }
}