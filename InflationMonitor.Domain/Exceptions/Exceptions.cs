namespace InflationMonitor.Domain.Exceptions {
    public abstract class DomainException(string message) : Exception(message) {
    }

    public class InvalidHistoricalPeriodException(string message) : DomainException($"Invalid historical period: {message}.") {
    }

    public class DomainArgumentOutOfRangeException(string parameterName, string message) : DomainException($"Invalid value for {parameterName}: {message}.") {
        public string ParamName { get; } = parameterName;
    }

    public class HistoricalDataNotFoundException(string message)
        : DomainException($"Historical data not found: {message}.") {
    }
}
