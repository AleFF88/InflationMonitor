using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InflationMonitor.Persistence.Configurations {
    public class InflationRateConfiguration : IEntityTypeConfiguration<InflationRate> {
        public void Configure(EntityTypeBuilder<InflationRate> builder) {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.Date })
                .IsUnique();

            builder.Property(x => x.Date)

                .IsRequired();
            builder.Property(x => x.Rate)
                .HasPrecision(18, 6)
                .IsRequired();
        }
    }
}
