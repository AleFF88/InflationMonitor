using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InflationMonitor.Persistence.Configurations {
    public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate> {
        public void Configure(EntityTypeBuilder<ExchangeRate> builder) {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.CurrencyCode, x.Year, x.Month })
                .IsUnique();

            builder.Property(x => x.CurrencyCode)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(x => x.Year)
                .IsRequired();

            builder.Property(x => x.Month)
                .IsRequired();

            builder.Property(x => x.Rate)
                .HasPrecision(18, 6)
                .IsRequired();
        }
    }
}
