using Microsoft.EntityFrameworkCore;
using Serilog;
using Spyglass.Core.Database;

namespace Spyglass.Core
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var startup = new Startup(builder.Configuration);
            
            // Setup logging to use Serilog.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            builder.Host.UseSerilog();
            builder.Host.UseSystemd();

            // Configure Dependency Injection services.
            startup.ConfigureServices(builder.Environment, builder.Services);
            
            // Configure the web application itself before running it.
            var app = builder.Build();
            startup.Configure(app);
            
            // Migrate the database if required.
            using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var dbContext = scope.ServiceProvider.GetRequiredService<SpyglassContext>())
                {
                    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        await dbContext.Database.MigrateAsync();
                    }
                }
            }
            
            await app.RunAsync();
        }
    }
}