using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InflationMonitor.Persistence {
    public class ApplicationDbContext : DbContext {
        public DbSet<InflationRate> InflationRates => Set<InflationRate>();
         public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
