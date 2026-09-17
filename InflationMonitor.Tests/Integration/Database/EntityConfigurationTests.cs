using FluentAssertions;
using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Domain.Entities;
using InflationMonitor.Persistence;
using InflationMonitor.Tests.Integration.WebApplicationFactory;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InflationMonitor.Tests.Integration.Database {
    /// <summary>
    /// Integration tests for Entity Framework Core entity configurations and database constraints.
    /// Verifies that unique constraints, indexes, and mapping rules are correctly enforced at the database level.
    /// </summary>
    public class EntityConfigurationTests : IClassFixture<CustomWebApplicationFactory> {
        private readonly CustomWebApplicationFactory _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityConfigurationTests"/> class.
        /// </summary>
        /// <param name="factory">The WebApplicationFactory fixture for managing the test server state.</param>
        public EntityConfigurationTests(CustomWebApplicationFactory factory) {
            _factory = factory;
        }

        /// <summary>
        /// Verifies that inserting duplicate records with the same currency code and date 
        /// violates the unique constraint and throws a <see cref="DbUpdateException"/>.
        /// </summary>
        [Fact]
        public async Task ExchangeRateConfiguration_WhenDuplicateCurrencyAndDateInserted_ShouldThrowDbUpdateException() {
            // Arrange
            await _factory.SeedDataAsync();
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var date = new DateOnly(2023, 1, 1);
            var rate1 = new ExchangeRate(CurrencyConstants.Codes.Usd, date, 36.5m);
            var rate2 = new ExchangeRate(CurrencyConstants.Codes.Usd, date, 37.0m);

            dbContext.ExchangeRates.Add(rate1);
            await dbContext.SaveChangesAsync();

            // Act
            dbContext.ExchangeRates.Add(rate2);
            Func<Task> act = async () => await dbContext.SaveChangesAsync();

            // Assert
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.WithInnerExceptionExactly<SqliteException>()
                .Which.SqliteErrorCode.Should().Be(19); // SQLite 19 = UNIQUE constraint failed
        }

        /// <summary>
        /// Verifies that inserting duplicate inflation records with the same date 
        /// violates the unique constraint and throws a <see cref="DbUpdateException"/>.
        /// </summary>
        [Fact]
        public async Task InflationRateConfiguration_WhenDuplicateDateInserted_ShouldThrowDbUpdateException() {
            // Arrange
            await _factory.SeedDataAsync();
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var date = new DateOnly(2023, 1, 1);
            var rate1 = new InflationRate(date, 1.01m);
            var rate2 = new InflationRate(date, 1.02m);

            dbContext.InflationRates.Add(rate1);
            await dbContext.SaveChangesAsync();

            // Act
            dbContext.InflationRates.Add(rate2);
            Func<Task> act = async () => await dbContext.SaveChangesAsync();

            // Assert
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.WithInnerExceptionExactly<SqliteException>()
                .Which.SqliteErrorCode.Should().Be(19); // SQLite 19 = UNIQUE constraint failed
        }
    }
}