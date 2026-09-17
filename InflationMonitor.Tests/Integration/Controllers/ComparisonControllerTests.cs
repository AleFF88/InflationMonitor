using FluentAssertions;
using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Application.Dtos;
using InflationMonitor.Domain.Entities;
using InflationMonitor.Tests.Integration.WebApplicationFactory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace InflationMonitor.Tests.Integration.Controllers {
    /// <summary>
    /// Integration tests for the comparison calculator endpoint.
    /// Verifies the HTTP API responses and underlying calculation logic against seeded database states.
    /// </summary>
    public class ComparisonControllerTests : IClassFixture<CustomWebApplicationFactory> {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComparisonControllerTests"/> class.
        /// </summary>
        /// <param name="factory">The WebApplicationFactory fixture for managing the test server state.</param>
        public ComparisonControllerTests(CustomWebApplicationFactory factory) {
            _factory = factory;
            _client = factory.CreateClient();
        }

        /// <summary>
        /// Verifies that the endpoint returns 200 OK and accurate calculated equivalents 
        /// when full dataset for inflation and currencies is available.
        /// </summary>
        [Fact]
        public async Task Compare_ShouldReturnOkAndCorrectCalculationResult() {

            // Arrange: Prepare test datasets for inflation and exchange rates
            var inflationRates = new[] {
                new InflationRate(new DateOnly(2023, 1, 1), 1.01m),
                new InflationRate(new DateOnly(2023, 2, 1), 1.02m)
            };

            var exchangeRates = new[] {
                new ExchangeRate(CurrencyConstants.Codes.Usd, new DateOnly(2023, 1, 1), 36.5m),
                new ExchangeRate(CurrencyConstants.Codes.Usd, new DateOnly(2023, 2, 1), 37.0m),
                new ExchangeRate(CurrencyConstants.Codes.Eur, new DateOnly(2023, 1, 1), 40.0m),
                new ExchangeRate(CurrencyConstants.Codes.Eur, new DateOnly(2023, 2, 1), 41.0m)
            };

            // Seed database using the web application factory helper
            await _factory.SeedDataAsync(inflationRates, exchangeRates);

            // Act: Send request to the comparison API endpoint
            var response = await _client.GetAsync("/api/calculator/compare?startDate=2023-01-01&endDate=2023-02-01&amount=1000");

            // Assert: Verify HTTP status code and response payload calculations
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<CalculateComparisonResponseDto>();

            result.Should().NotBeNull();

            result!.InitialAmount.Should().Be(1000m);

            result.Summary.CashGrivna.Should().Be(1000m);
            result.Summary.InflationEquivalent.Should().Be(1030.20m);
            result.Summary.UsdEquivalent.Should().Be(1013.70m);
            result.Summary.EurEquivalent.Should().Be(1025.00m);

        }

        /// <summary>
        /// Verifies that the endpoint returns 200 OK with appropriate warning messages 
        /// when some requested currency data is missing in the database.
        /// </summary>
        [Fact]
        public async Task CalculateComparison_WhenCurrencyDataIsPartial_ReturnsOkWithWarnings() {
            // Arrange: Prepare incomplete datasets
            var inflationRates = new[] {
                new InflationRate(new DateOnly(2023, 1, 1), 1.01m),
                new InflationRate(new DateOnly(2023, 2, 1), 1.02m)
            };

            var exchangeRates = new[] {
                new ExchangeRate(CurrencyConstants.Codes.Usd, new DateOnly(2023, 1, 1), 36.5m),
                new ExchangeRate(CurrencyConstants.Codes.Usd, new DateOnly(2023, 2, 1), 37.0m) 
                // Data for EUR is intentionally not added.
            };

            // Seed database with partial data
            await _factory.SeedDataAsync(inflationRates, exchangeRates);

            // Act: Perform the calculation request
            var response = await _client.GetAsync("/api/calculator/compare?startDate=2023-01-01&endDate=2023-02-01&amount=1000");

            // Assert: Verify successful response status alongside warning information for missing EUR
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<CalculateComparisonResponseDto>();

            result.Should().NotBeNull();

            result!.Summary.UsdEquivalent.Should().Be(1013.70m);
            result.Summary.InflationEquivalent.Should().Be(1030.20m);

            result.Summary.EurEquivalent.Should().BeNull();
            result.Warnings.Should().NotBeEmpty();
            result.Warnings.Should().Contain(w => w.Contains(CurrencyConstants.Codes.Eur));
        }

        /// <summary>
        /// Verifies that the endpoint returns 400 Bad Request and validation error details 
        /// when passed a negative or out-of-range amount query parameter.
        /// </summary>
        [Fact]
        public async Task Compare_ShouldReturnBadRequest_WhenAmountIsInvalid() {
            // Act: Send request with negative initial amount (amount=-100)
            var response = await _client.GetAsync("/api/calculator/compare?startDate=2023-01-01&endDate=2023-02-01&amount=-100");

            // Assert: Verify problem details returned for validation error
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Validation Error");
        }

        /// <summary>
        /// Verifies that when an unhandled exception occurs in the request handling pipeline,
        /// the API returns HTTP 500 Internal Server Error with standardized ProblemDetails 
        /// and hides internal exception details from the response.
        /// </summary>
        [Fact]
        public async Task Compare_WhenUnhandledExceptionOccurs_ShouldReturn500InternalServerErrorWithGenericProblemDetails() {
            // Arrange: Create a custom client with a overridden strategy dependency that throws an unexpected exception
            var faultyStrategyMock = new Mock<IBatchFinancialInstrumentStrategy>();
            faultyStrategyMock.SetupGet(s => s.CategoryKey).Returns(FinancialInstrumentCategories.Inflation);
            faultyStrategyMock
                .Setup(s => s.CalculateEquivalentsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<decimal>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Sensitive internal database connection failed."));

            var clientWithFaultyStrategy = _factory.WithWebHostBuilder(builder => {
                builder.ConfigureTestServices(services => {
                    // Replace InflationStrategy with the faulty mock
                    var descriptors = services.Where(d => d.ServiceType == typeof(IBatchFinancialInstrumentStrategy)).ToList();
                    foreach (var descriptor in descriptors) {
                        services.Remove(descriptor);
                    }

                    services.AddScoped(_ => faultyStrategyMock.Object);
                });
            }).CreateClient();

            // Act
            var response = await clientWithFaultyStrategy.GetAsync("/api/calculator/compare?startDate=2023-01-01&endDate=2023-02-01&amount=1000");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(StatusCodes.Status500InternalServerError);
            problemDetails.Title.Should().Be("Server Error");
            problemDetails.Detail.Should().Be("An unexpected error occurred on the server.");

            // Ensure internal exception details are not leaked
            problemDetails.Detail.Should().NotContain("Sensitive internal database connection failed.");
        }

        /// <summary>
        /// Verifies that when an invalid query parameter is provided (e.g., amount = 0),
        /// the MediatR ValidationBehavior intercepts the pipeline and returns HTTP 400 Bad Request 
        /// with ProblemDetails containing structured validation errors.
        /// </summary>
        [Fact]
        public async Task Compare_WhenAmountIsZero_ShouldBeInterceptedByValidationBehaviorAndReturn400WithErrors() {
            // Act: Send request with invalid amount (amount = 0)
            var response = await _client.GetAsync("/api/calculator/compare?startDate=2023-01-01&endDate=2023-02-01&amount=0");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(StatusCodes.Status400BadRequest);
            problemDetails.Title.Should().Be("Validation Error");
            problemDetails.Detail.Should().Be("One or more validation failures have occurred.");

            // Verify structured error details dictionary for the 'Amount' parameter
            problemDetails.Extensions.Should().ContainKey("errors");

            var errorsJson = JsonSerializer.Serialize(problemDetails.Extensions["errors"]);
            var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsJson);

            errors.Should().NotBeNull();
            errors!.Should().ContainKey("Amount");
            errors["Amount"].Should().Contain("Amount must be greater than zero.");
        }
    }
}