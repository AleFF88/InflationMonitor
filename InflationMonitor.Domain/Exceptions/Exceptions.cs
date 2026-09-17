namespace InflationMonitor.Domain.Exceptions {
    /// <summary>
    /// Serves as the base abstract class for all domain-level exceptions within the application.
    /// </summary>
    public abstract class DomainException(string message) : Exception(message) { }

    /// <summary>
    /// The exception that is thrown when a requested historical period falls outside the supported boundaries.
    /// </summary>
    public class InvalidHistoricalPeriodException(string message) : DomainException(message) { }

    /// <summary>
    /// The exception that is thrown when a domain argument or parameter falls out of the allowable range or violates business rules.
    /// </summary>
    public class DomainArgumentOutOfRangeException : DomainException {
        /// <summary>
        /// Gets the name of the parameter that caused the out-of-range exception.
        /// </summary>
        public string ParamName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainArgumentOutOfRangeException"/> class with the specified parameter name and error message.
        /// </summary>
        /// <param name="paramName">The name of the parameter associated with this exception.</param>
        /// <param name="message">The message that describes the error.</param>
        public DomainArgumentOutOfRangeException(string paramName, string message)
            : base(message) { 
            ParamName = paramName; 
        }
    }
}