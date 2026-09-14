using FluentAssertions;
using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Dtos;
using InflationMonitor.Domain.Entities;
using InflationMonitor.Tests.Integration;
using System.Net;
using System.Net.Http.Json;

namespace InflationMonitorTests.Integration {
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

            var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Validation Error");
        }
    }
}