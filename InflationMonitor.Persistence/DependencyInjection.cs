using InflationMonitor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InflationMonitor.Persistence {
    public static class DependencyInjection {
        public static IServiceCollection AddPersistenceServices(
            this IServiceCollection services,
            IConfiguration configuration) {
            
            // Register Entity Framework DbContext with SQLite 						
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Registering DbContext with SQLite
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString)
            );

            // Map IApplicationDbContext interface to concrete ApplicationDbContext implementation 
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>()
            );

            return services;
        }
    }
}
