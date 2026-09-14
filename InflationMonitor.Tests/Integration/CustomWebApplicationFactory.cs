using InflationMonitor.Domain.Entities; 
using InflationMonitor.Persistence;
using InflationMonitor.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InflationMonitor.Tests.Integration {
    /// <summary>
    /// Custom factory for bootstrapping the test web server and setting up
    /// in-memory SQLite database isolation for integration tests.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program> {
        private SqliteConnection? _connection;

        /// <summary>
        /// Configures the web host for the testing environment,
        /// replacing the production database context with SQLite In-Memory.
        /// </summary>
        /// <param name="builder">The web host builder.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            // Explicitly set the environment to "Testing" to skip Development-only blocks (like auto-seeding)
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services => {

                // Locate and remove the original DbContextOptions registration from the main application
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) {
                    services.Remove(descriptor);
                }

                // Create and open a single SQLite in-memory connection (kept open to prevent DB deletion)
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                // Register ApplicationDbContext using the SQLite In-Memory connection
                services.AddDbContext<ApplicationDbContext>(options => {
                    options.UseSqlite(_connection);
                });

                // Build a temporary ServiceProvider for initial database schema setup
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Ensure the database schema is created based on EF Core models
                db.Database.EnsureCreated();
            });
        }

        /// <summary>
        /// Asynchronously clears existing database state and seeds provided test datasets.
        /// </summary>
        /// <param name="inflationRates">Optional collection of inflation rates to insert.</param>
        /// <param name="exchangeRates">Optional collection of exchange rates to insert.</param>
        /// <returns>A task representing the asynchronous seed operation.</returns>
        public async Task SeedDataAsync(IEnumerable<InflationRate>? inflationRates = null, IEnumerable<ExchangeRate>? exchangeRates = null) {
            // Create an isolated service scope for database operations
            using var scope = Services.CreateScope(); 
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Completely clear existing records from test tables
            dbContext.InflationRates.RemoveRange(dbContext.InflationRates); 
            dbContext.ExchangeRates.RemoveRange(dbContext.ExchangeRates); 
            await dbContext.SaveChangesAsync();

            // Insert new inflation rates if provided
            if (inflationRates != null) { 
                dbContext.InflationRates.AddRange(inflationRates); 
            }

            // Insert new exchange rates if provided
            if (exchangeRates != null) { 
                dbContext.ExchangeRates.AddRange(exchangeRates); 
            }

            // Persist changes to the database
            await dbContext.SaveChangesAsync(); 
        }

        /// <summary>
        /// Releases managed and unmanaged resources used by the factory and closes the SQLite connection.
        /// </summary>
        /// <param name="disposing">True to release managed resources.</param>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}