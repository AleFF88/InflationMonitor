using InflationMonitor.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InflationMonitor.Tests.Helpers {
    /// <summary>
    /// Factory helper providing pre-configured <see cref="ApplicationDbContext"/> instances
    /// backed by an open SQLite In-Memory database connection for unit and integration testing.
    /// </summary>
    public static class DbContextFactory {
        /// <summary>
        /// Creates and initializes an <see cref="ApplicationDbContext"/> with an open SQLite In-Memory connection.
        /// Ensures the underlying schema is created before returning.
        /// </summary>
        /// <returns>A tuple containing the created DbContext and the open SqliteConnection instance.</returns>
        public static (ApplicationDbContext Context, SqliteConnection Connection) CreateInMemoryDbContext() {
            // Create and open a single SQLite in-memory connection (kept open to prevent DB deletion)
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);

            // Ensure the database schema is created based on EF Core models
            context.Database.EnsureCreated();

            return (context, connection);
        }
    }
}