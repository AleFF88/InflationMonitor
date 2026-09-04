using FluentValidation;
using InflationMonitor.Application.Common.Behaviors;
using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Application.Dtos;
using InflationMonitor.Application.Queries.CalculateComparison;
using InflationMonitor.Persistence;
using InflationMonitor.Persistence.Seeding;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InflationMonitor.WebApi {
    public class Program {
        public static async Task Main(string[] args) {
            // Initialize the web application builder with command-line arguments and default configurations
            var builder = WebApplication.CreateBuilder(args);

            // Add support for API controllers to the DI container
            builder.Services.AddControllers();
            // Register API Explorer and Swagger generator services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add FluentValidation validatiors to the DI container
            builder.Services.AddValidatorsFromAssemblyContaining<CalculateComparisonQueryValidator>();

            // Register MediatR and connect ValidationBehavior to the MediatR Execution Pipeline
            builder.Services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(CalculateComparisonResponseDto).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            // Register Entity Framework DbContext with SQLite 						
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString)
            );

            // Map IApplicationDbContext interface to concrete ApplicationDbContext implementation 
            builder.Services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>()
            );

            // Build the WebApplication instance using the configured services
            var app = builder.Build();

            // For Development environment
            if (app.Environment.IsDevelopment()) {

                // Automatic data seeding in development environment (for testing and development purposes
                using (var scope = app.Services.CreateScope()) {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await DbContextSeeder.SeedAsync(dbContext);
                }

                // Enable Swagger UI
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Enforce HTTPS redirection middleware to route HTTP requests to HTTPS
            app.UseHttpsRedirection();
            // Enable authorization middleware to process user permissions on endpoints
            app.UseAuthorization();
            // Map incoming HTTP requests to corresponding controller action routes
            app.MapControllers();

            // Run the application and start listening for incoming HTTP requests
            await app.RunAsync();
        }
    }
}
