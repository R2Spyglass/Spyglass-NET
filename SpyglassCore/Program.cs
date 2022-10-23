using Serilog;

namespace Spyglass.Core
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var startup = new Startup(builder.Configuration);
            
            // Setup logging to use Serilog.
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            Log.Logger = logger;

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(logger);
            
            // Configure Dependency Injection services.
            startup.ConfigureServices(builder.Services);
            
            // Configure the web application itself before running it.
            var app = builder.Build();
            startup.Configure(app);
            
            await app.RunAsync();
        }
    }
}