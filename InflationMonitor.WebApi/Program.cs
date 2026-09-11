using InflationMonitor.Application;
using InflationMonitor.Persistence;
using InflationMonitor.Persistence.Seeding;

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

            // Register Application layer services (MediatR, FluentValidation, Strategies)
            builder.Services.AddApplicationServices();

            // Register Persistence layer services (DbContext, SQLite)
            builder.Services.AddPersistenceServices(builder.Configuration);

            // Register global exception handler and problem details middleware
            builder.Services.AddExceptionHandler<Common.GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            // Register in-memory caching services with custom options
            builder.Services.AddMemoryCache(options => { 
                options.SizeLimit = 22_000;   
                options.CompactionPercentage = 0.2;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(15); 
            });

            // Build the WebApplication instance using the configured services
            var app = builder.Build();

            // Use the global exception handler middleware
            app.UseExceptionHandler();

            // For Development environment
            if (app.Environment.IsDevelopment()) {

                // Automatic data seeding in development environment (for testing and development purposes)
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
