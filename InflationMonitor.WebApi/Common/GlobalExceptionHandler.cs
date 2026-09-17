using FluentValidation;
using InflationMonitor.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InflationMonitor.WebApi.Common {
    /// <summary>
    /// Represents a global exception handler that intercepts unhandled exceptions, 
    /// logs them, and formats them into standard RFC 7807 ProblemDetails responses.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used to log unhandled exceptions.</param>
        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) {
            _logger = logger;
        }

        /// <inheritdoc />
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken) {

            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            var (statusCode, title, detail, errors) = MapException(exception);

            var problemDetails = new ProblemDetails {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };

            if (errors != null) {
                problemDetails.Extensions["errors"] = errors;
            }

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }

        private static (int StatusCode, string Title, string Detail, IDictionary<string, string[]>? Errors) MapException(Exception exception) {

            return exception switch {
                ValidationException validationEx => (
                    StatusCode : StatusCodes.Status400BadRequest,
                    Title : "Validation Error",
                    Detail : "One or more validation failures have occurred.",
                    Errors : validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                ),

                DomainException domainEx => (
                    StatusCode : StatusCodes.Status400BadRequest,
                    Title : "Domain Error",
                    Detail : domainEx.Message,
                    Errors : null
                ),

                _ => (
                    StatusCode : StatusCodes.Status500InternalServerError,
                    Title : "Server Error",
                    Detail : "An unexpected error occurred on the server.",
                    Errors : null
                )
            };
        }
    }
}