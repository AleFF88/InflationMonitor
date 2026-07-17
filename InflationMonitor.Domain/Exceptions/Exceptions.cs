namespace InflationMonitor.Domain.Exceptions {
    public abstract class DomainException : Exception {
        protected DomainException(string message) : base(message) { }
    }

    public class InvalidHistoricalPeriodException : DomainException {
        public InvalidHistoricalPeriodException(string message) : base($"Invalid historical period: {message}.") { }
    }

    public class DomainArgumentOutOfRangeException : DomainException {
        public string ParamName { get; } 
        public DomainArgumentOutOfRangeException(string parameterName, string message) : base($"Invalid value for {parameterName}: {message}.") {
            ParamName = parameterName;
        }
    }
}
