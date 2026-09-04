namespace InflationMonitor.Domain.Exceptions {
    public abstract class DomainException(string message) : Exception(message) { }

    public class InvalidHistoricalPeriodException(string message) : DomainException(message) { }

    public class DomainArgumentOutOfRangeException : DomainException {
        public string ParamName { get; }

        public DomainArgumentOutOfRangeException(string paramName, string message)
            : base(message) {
            ParamName = paramName;
        }
    }
}