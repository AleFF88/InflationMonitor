using InflationMonitor.Application.Common.Interfaces;

namespace InflationMonitor.Application.Factories {
    public class FinancialInstrumentFactory : IFinancialInstrumentFactory {
        private readonly IDictionary<string, IBatchFinancialInstrumentStrategy> _strategies;

        public FinancialInstrumentFactory(IEnumerable<IBatchFinancialInstrumentStrategy> strategies) {
            var dictionary = new Dictionary<string, IBatchFinancialInstrumentStrategy>(StringComparer.OrdinalIgnoreCase);

            foreach (var strategy in strategies) {
                var key = strategy.CategoryKey; 
                dictionary.Add(key, strategy);
            }

            _strategies = dictionary;
        }

        public IBatchFinancialInstrumentStrategy GetStrategy(string categoryKey) {
            if (_strategies.TryGetValue(categoryKey, out var strategy)) {
                return strategy;
            }

            throw new NotSupportedException($"Financial instrument category '{categoryKey}' is not supported.");
        }
    }
}