using InflationMonitor.Persistence;
using InflationMonitor.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;

namespace InflationMonitor.Tests.Unit.Application.Strategies {
    /// <summary>
    /// Abstract base class for unit tests targeting strategy implementations.
    /// Manages an isolated SQLite In-Memory database instance and an <see cref="IMemoryCache"/> lifecycle per test.
    /// </summary>
    public abstract class StrategyTestBase : IDisposable {
        private readonly SqliteConnection _connection;
        private bool _disposed;
        protected readonly ApplicationDbContext DbContext;
        protected readonly IMemoryCache MemoryCache;    // Isolated in-memory cache instance for evaluating strategy caching logic.

        protected StrategyTestBase() {
            var (context, connection) = DbContextFactory.CreateInMemoryDbContext();
            DbContext = context;
            _connection = connection;
            MemoryCache = new MemoryCache(new MemoryCacheOptions());     // Initializing a fresh MemoryCache instance for each individual test
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                DbContext.Dispose();
                MemoryCache.Dispose();
                _connection.Close();
                _connection.Dispose();
            }

            _disposed = true;
        }
    }
}