using InflationMonitor.Application;
using InflationMonitor.Persistence;
using InflationMonitor.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;

namespace InflationMonitor.WebApi {
    public class Program {
        public static async Task Main(string[] args) {
            // Initialize the web application builder with command-line arguments
            var builder = WebApplication.CreateBuilder(args);

            // Add support for API controllers to the DI container
            builder.Services.AddControllers();

            // Register API Explorer and Swagger generator services
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options => {
                options.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "Inflation Monitor API",
                    Version = "v1",
                    Description = "API for calculating changes in the purchasing power of the Ukrainian Hryvnia relative to various financial equivalents based on historical data."
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

            // Register Application layer services (MediatR, FluentValidation, Strategies)
            builder.Services.AddApplicationServices();

            // Register Persistence layer services (DbContext, SQLite)
            builder.Services.AddPersistenceServices(builder.Configuration);

            // Register global exception handler and problem details middleware
            builder.Services.AddExceptionHandler<Common.GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            // Build the WebApplication instance using the configured services
            var app = builder.Build();

            // Use the global exception handler middleware
            app.UseExceptionHandler();

            // For Development environment
            if (app.Environment.IsDevelopment()) {

                // Automatic data seeding in development environment (for testing and development purposes)
                using (var scope = app.Services.CreateScope()) {
                    // Resolve ApplicationDbContext instance from the service provider scope
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Applies any pending migrations for the context to the database.
                    //   Will create the database and tables if they do not exist yet.
                    await dbContext.Database.MigrateAsync();

                    // Seed initial historical financial data into the database if tables are empty
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
