using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using InflationMonitor.Domain.Exceptions;
using InflationMonitor.WebApi.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace InflationMonitor.Tests.Unit.WebApi {
    /// <summary>
    /// Unit tests for verifying the behavior of <see cref="GlobalExceptionHandler"/>.
    /// </summary>
    public class GlobalExceptionHandlerTests {
        private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
        private readonly GlobalExceptionHandler _handler;

        public GlobalExceptionHandlerTests() {
            _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
            _handler = new GlobalExceptionHandler(_loggerMock.Object);
        }

        /// <summary>
        /// Verifies that <see cref="ValidationException"/> produces HTTP 400 Bad Request 
        /// with ProblemDetails containing validation error mapping grouped by field names.
        /// </summary>
        [Fact]
        public async Task TryHandleAsync_WhenValidationExceptionThrown_ShouldReturn400BadRequestWithProblemDetailsAndErrors() {
            // Arrange
            var httpContext = CreateHttpContext();
            var validationFailures = new List<ValidationFailure> {
                new("StartDate", "StartDate cannot be later than EndDate."),
                new("Amount", "Amount must be greater than zero.")
            };
            var exception = new ValidationException(validationFailures);

            // Act
            var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
            var problemDetails = await ReadProblemDetailsAsync(httpContext);

            // Assert
            result.Should().BeTrue();
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Validation Error");
            problemDetails.Detail.Should().Be("One or more validation failures have occurred.");

            problemDetails.Extensions.Should().ContainKey("errors");
            var errorsJson = JsonSerializer.Serialize(problemDetails.Extensions["errors"]);
            var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsJson);

            errors.Should().NotBeNull();
            errors!.Should().ContainKey("StartDate"); 
            errors!.Should().ContainKey("Amount"); 
        }

        /// <summary>
        /// Verifies that <see cref="DomainException"/> produces HTTP 400 Bad Request with ProblemDetails
        /// passing the domain exception message directly into the detail field.
        /// </summary>
        [Fact]
        public async Task TryHandleAsync_WhenDomainExceptionThrown_ShouldReturn400BadRequestWithDomainErrorMessage() {
            // Arrange
            var httpContext = CreateHttpContext();
            const string domainErrorMessage = "Historical financial data is unavailable for the requested period.";
            var exception = new InvalidHistoricalPeriodException(domainErrorMessage);

            // Act
            var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
            var problemDetails = await ReadProblemDetailsAsync(httpContext);

            // Assert
            result.Should().BeTrue();
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Domain Error");
            problemDetails.Detail.Should().Be(domainErrorMessage);
        }

        /// <summary>
        /// Verifies that unhandled exceptions produce HTTP 500 Internal Server Error with a 
        /// generic error message,avoiding sensitive internal stack trace leakage.
        /// </summary>
        [Fact]
        public async Task TryHandleAsync_WhenUnhandledExceptionThrown_ShouldReturn500InternalServerErrorWithGenericMessage() {
            // Arrange
            var httpContext = CreateHttpContext();
            var exception = new InvalidOperationException("Sensitive internal database connection failed.");

            // Act
            var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
            var problemDetails = await ReadProblemDetailsAsync(httpContext);

            // Assert
            result.Should().BeTrue();
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Server Error");
            problemDetails.Detail.Should().Be("An unexpected error occurred on the server.");
            problemDetails.Detail.Should().NotContain("Sensitive internal database connection failed."); 
        }

        /// <summary>
        /// Verifies that any processed exception is properly logged with error level
        /// and exception details.
        /// </summary>
        [Fact]
        public async Task TryHandleAsync_WhenExceptionOccurs_ShouldLogErrorWithExceptionDetails() {
            // Arrange
            var httpContext = CreateHttpContext();
            var exception = new InvalidOperationException("Test exception for logging verification.");

            // Act
            await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="GlobalExceptionHandler.TryHandleAsync"/> always returns true
        /// and writes serialized ProblemDetails content to the HTTP response body.
        /// </summary>
        [Fact]
        public async Task TryHandleAsync_Always_ShouldReturnTrueAndWriteProblemDetailsToResponseBody() {
            // Arrange
            var httpContext = CreateHttpContext();
            var exception = new Exception("Any exception");

            // Act
            var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            httpContext.Response.Body.Length.Should().BeGreaterThan(0); 
        }

        private static DefaultHttpContext CreateHttpContext() {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static async Task<ProblemDetails?> ReadProblemDetailsAsync(HttpContext context) {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var bodyText = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<ProblemDetails>(bodyText, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}