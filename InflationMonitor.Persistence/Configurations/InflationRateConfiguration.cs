using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InflationMonitor.Persistence.Configurations {
    public class InflationRateConfiguration : IEntityTypeConfiguration<InflationRate> {
        public void Configure(EntityTypeBuilder<InflationRate> builder) {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Year)
                .IsRequired();

            builder.Property(x => x.Month)
                .IsRequired();

            builder.Property(x => x.Value)
                .HasPrecision(18, 6)
                .IsRequired();
        }
    }
}
