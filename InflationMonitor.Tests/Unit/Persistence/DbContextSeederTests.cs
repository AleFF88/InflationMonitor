using FluentAssertions;
using InflationMonitor.Persistence.Seeding;
using InflationMonitor.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace InflationMonitor.Tests.Integration.Persistence {
    /// <summary>
    /// Integration tests for <see cref="DbContextSeeder"/> verification.
    /// Ensures initial data seeding, idempotency, and missing file exception handling.
    /// </summary>
    public class DbContextSeederTests {
        /// <summary>
        /// Verifies that calling SeedAsync on an empty database successfully seeds initial
        /// inflation and exchange rates.
        /// </summary>
        [Fact]
        public async Task SeedAsync_WhenDatabaseIsEmpty_ShouldSeedInitialData() {
            // Arrange
            var (context, connection) = DbContextFactory.CreateInMemoryDbContext();

            try {
                // Act
                await DbContextSeeder.SeedAsync(context);

                // Assert
                var inflationCount = await context.InflationRates.CountAsync();
                var exchangeCount = await context.ExchangeRates.CountAsync();

                inflationCount.Should().BeGreaterThan(0);
                exchangeCount.Should().BeGreaterThan(0);
            }
            finally {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that calling SeedAsync repeatedly on an already seeded database 
        /// does not duplicate records or throw unexpected exceptions.
        /// </summary>
        [Fact]
        public async Task SeedAsync_WhenDatabaseIsAlreadySeeded_ShouldBeIdempotentAndNotDuplicateData() {
            // Arrange
            var (context, connection) = DbContextFactory.CreateInMemoryDbContext();

            try {
                // Initial seeding pass
                await DbContextSeeder.SeedAsync(context);
                var initialInflationCount = await context.InflationRates.CountAsync();
                var initialExchangeCount = await context.ExchangeRates.CountAsync();

                // Act: Perform secondary seeding pass
                Func<Task> act = async () => await DbContextSeeder.SeedAsync(context);

                // Assert
                await act.Should().NotThrowAsync();

                var secondInflationCount = await context.InflationRates.CountAsync();
                var secondExchangeCount = await context.ExchangeRates.CountAsync();

                secondInflationCount.Should().Be(initialInflationCount);
                secondExchangeCount.Should().Be(initialExchangeCount);
            }
            finally {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that calling SeedAsync throws a FileNotFoundException when required JSON 
        /// seeding files are missing.
        /// </summary>
        [Fact]
        public async Task SeedAsync_WhenSeedingDirectoryIsMissing_ShouldThrowFileNotFoundException() {
            // Arrange
            var (context, connection) = DbContextFactory.CreateInMemoryDbContext();

            var seedingDir = Path.Combine(AppContext.BaseDirectory, "Seeding", "Data");
            var tempBackupDir = Path.Combine(AppContext.BaseDirectory, "Seeding", "Data_BackupTemp");

            try {
                // Temporarily rename seeding data directory if it exists to simulate missing files
                if (Directory.Exists(seedingDir)) {
                    Directory.Move(seedingDir, tempBackupDir);
                }

                // Act
                Func<Task> act = async () => await DbContextSeeder.SeedAsync(context);

                // Assert
                await act.Should().ThrowAsync<FileNotFoundException>()
                    .WithMessage("*Seeding file not found at path*");
            }
            finally {
                // Restore seeding directory
                if (Directory.Exists(tempBackupDir)) {
                    Directory.Move(tempBackupDir, seedingDir);
                }

                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}