using InflationMonitor.Domain.Entities;

namespace InflationMonitor.Persistence.Seeding {
    public static class DbContextSeeder {
        public static void Seed(ApplicationDbContext context) {

            if (!context.InflationRates.Any()) {
                var inflationData = new[] {
                    // Just for testing purposes.
                    // TODO replace this with real data.
                    new InflationRate(2021, 1, 1.013m),
                    new InflationRate(2021, 2, 1.010m),
                    new InflationRate(2021, 3, 1.017m),
                };
                context.InflationRates.AddRange(inflationData);
            }


            if (!context.ExchangeRates.Any()) {
                var exchangeData = new[] {
                    // Just for testing purposes.
                    // TODO replace this with real data.
                    new ExchangeRate("USD", 2021, 1, 28.19m),
                    new ExchangeRate("USD", 2021, 2, 27.88m),
                    new ExchangeRate("USD", 2021, 3, 27.97m),
                };
                context.ExchangeRates.AddRange(exchangeData);
            }

            context.SaveChanges();
        }   
    }
}
