using FluentAssertions;
using InflationMonitor.Application.Dtos;
using InflationMonitor.Domain.Entities;
using InflationMonitor.Persistence;
using InflationMonitor.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http.Json;

namespace InflationMonitorTests.Integration {
    public class ComparisonControllerTests : IClassFixture<CustomWebApplicationFactory> {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public ComparisonControllerTests(CustomWebApplicationFactory factory) {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Compare_ShouldReturnOkAndCorrectCalculationResult() {

            // Arrange
            using (var scope = _factory.Services.CreateScope()) {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                dbContext.InflationRates.RemoveRange(dbContext.InflationRates);
                dbContext.ExchangeRates.RemoveRange(dbContext.ExchangeRates);
                await dbContext.SaveChangesAsync();

                dbContext.InflationRates.AddRange(
                    new InflationRate(2023, 1, 1.01m),
                    new InflationRate(2023, 2, 1.02m)
                );

                dbContext.ExchangeRates.AddRange(
                    new ExchangeRate("USD", 2023, 1, 36.5m),
                    new ExchangeRate("USD", 2023, 2, 37.0m),
                    new ExchangeRate("EUR", 2023, 1, 40.0m),
                    new ExchangeRate("EUR", 2023, 2, 41.0m)
                );

                await dbContext.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/api/calculator/compare?startDate=2023-01-01&endDate=2023-02-01&amount=1000");

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<CalculateComparisonResponseDto>();

            result.Should().NotBeNull();

            result!.InitialAmount.Should().Be(1000m);

            result.Summary.CashGrivna.Should().Be(1000m);
            result.Summary.InflationEquivalent.Should().Be(1030.20m);
            result.Summary.UsdEquivalent.Should().Be(1013.70m);
            result.Summary.EurEquivalent.Should().Be(1025.00m);

        }
    }
}