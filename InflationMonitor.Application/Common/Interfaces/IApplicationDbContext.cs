using InflationMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InflationMonitor.Application.Common.Interfaces {
    public interface IApplicationDbContext {
        DbSet<InflationRate> InflationRates { get; }
        DbSet<ExchangeRate> ExchangeRates { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}