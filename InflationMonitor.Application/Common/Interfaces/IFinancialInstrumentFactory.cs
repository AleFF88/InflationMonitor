namespace InflationMonitor.Application.Common.Interfaces {
    public interface IFinancialInstrumentFactory {
        IBatchFinancialInstrumentStrategy GetStrategy(string categoryKey);
    }
}
