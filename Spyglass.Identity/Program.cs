using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace Spyglass.Identity
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

            startup.ConfigureServices(builder, builder.Services);
            var app = builder.Build();
            startup.Configure(app);

            using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                var pendingMigrations = await configContext.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.Warning("IdentityServer4 Configuration Database is out of date, migrating");
                    await configContext.Database.MigrateAsync();
                }

                if (!configContext.ApiScopes.Any())
                {
                    logger.Information("Adding API scopes to the database");
                    configContext.ApiScopes.AddRange(AuthorizationConfig.Scopes.Select(s => s.ToEntity()));
                }
                
                if (!configContext.ApiResources.Any())
                {
                    logger.Information("Adding API resources to the database");
                    configContext.ApiResources.AddRange(AuthorizationConfig.ApiResources(builder.Configuration).Select(a => a.ToEntity()));
                    await configContext.SaveChangesAsync();
                }
                
                if (!configContext.Clients.Any())
                {
                    logger.Warning("No clients registered in the database, adding Spyglass admin client");

                    var secret = Convert.ToBase64String(CryptoRandom.CreateRandomKey(32));
                    var adminClient = new Client
                    {
                        ClientId = AuthorizationConfig.SpyglassAdminClientId,
                        ClientName = "SpyglassAdmin",
                        ClientSecrets = new List<Secret>
                        {
                            new Secret(secret.Sha256(), "Spyglass Admin Secret")
                        },
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        AllowedScopes = AuthorizationConfig.ApiResources(builder.Configuration)
                            .SelectMany(s => s.Scopes)
                            .ToList(),
                        AccessTokenType = AccessTokenType.Reference,
                        AccessTokenLifetime = int.MaxValue
                    };

                    var clientInfo = new
                    {
                        ClientId = adminClient.ClientId,
                        ClientSecret = secret,
                    };
                    
                    configContext.Clients.Add(adminClient.ToEntity());
                    await configContext.SaveChangesAsync();
                    
                    logger.Information("Created Spyglass admin client, saving credentials to danger/spyglass-credentials.json");
                    var json = JsonConvert.SerializeObject(clientInfo, Formatting.Indented);
                    
                    Directory.CreateDirectory("danger");
                    await File.WriteAllTextAsync("danger/spyglass-credentials.json", json);
                }
                
                var persistedContext = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
                var persistedMigrations = await persistedContext.Database.GetPendingMigrationsAsync();

                if (persistedMigrations.Any())
                {
                    logger.Warning("IdentityServer4 Persisted Grant Database is out of date, migrating");
                    await persistedContext.Database.MigrateAsync();
                }
            }

            logger.Information("Setup is complete, running identity server");
            await app.RunAsync(AuthorizationConfig.IdentityServerUrl);
        }
    }
}

