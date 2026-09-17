using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

namespace InflationMonitor.Persistence.Seeding {
    /// <summary>
    /// Provides functionality for seeding initial historical inflation and exchange rate data 
    /// from JSON files into the database during application startup.
    /// </summary>
    public static class DbContextSeeder {
        private static readonly JsonSerializerOptions JsonOptions = new() {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Asynchronously seeds the database with initial dataset records if the corresponding tables are empty.
        /// </summary>
        /// <param name="context">The application database context instance.</param>
        /// <returns>A task representing the asynchronous seeding operation.</returns>
        public static async Task SeedAsync(ApplicationDbContext context) {
            // Seeding the inflation rates.
            await SeedDataAsync<InflationRate>(context.InflationRates, "inflation.json");

            // Seeding the USD exchange rates: the ratio of UAH to 1 USD
            await SeedDataAsync<ExchangeRate>(context.ExchangeRates, "usd.json", x => x.CurrencyCode == CurrencyConstants.Codes.Usd);

            // Seeding the EUR exchange rates: the ratio of UAH to 1 EUR
            await SeedDataAsync<ExchangeRate>(context.ExchangeRates, "eur.json", x => x.CurrencyCode == CurrencyConstants.Codes.Eur);

            await context.SaveChangesAsync();
        }

        private static async Task SeedDataAsync<TEntity>(
            DbSet<TEntity> dbSet,
            string fileName,
            Expression<Func<TEntity, bool>>? predicate = null
        ) where TEntity : class {

            // Check if the table already contains data to prevent duplicate seeding
            var hasData = predicate != null
                ? await dbSet.AnyAsync(predicate)
                : await dbSet.AnyAsync();
            if (hasData) { return; }

            var filePath = Path.Combine(AppContext.BaseDirectory, "Seeding", "Data", fileName);

            if (!File.Exists(filePath)) {
                throw new FileNotFoundException($"Seeding file not found at path: {filePath}");
            }

            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize<List<TEntity>>(json, JsonOptions);

            if (data == null || data.Count == 0) {
                throw new InvalidOperationException($"Failed to deserialize {typeof(TEntity).Name} or file '{filePath}' is empty.");
            }

            dbSet.AddRange(data);
        }
    }
}