namespace InflationMonitor.WebApi {
    public class Program {
        public static void Main(string[] args) {
            // Initialize the web application builder with command-line arguments and default configurations
            var builder = WebApplication.CreateBuilder(args);

            // Add support for API controllers to the Dependency Injection container
            builder.Services.AddControllers();
            // Register API Explorer and Swagger generator services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Build the WebApplication instance using the configured services
            var app = builder.Build();

            // 6. Enable Swagger UI in Development environment
            if (app.Environment.IsDevelopment()) {
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
            app.Run();
        }
    }
}
