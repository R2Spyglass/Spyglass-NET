using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Spyglass.Identity
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        
        public Startup(IConfigurationRoot configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(WebApplicationBuilder builder, IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            // Setup IdentityServer for authentication.
            var identity = services.AddIdentityServer()
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = db => db.UseNpgsql(Configuration.GetConnectionString("IdentityServerContext"),
                        pg =>
                        {
                            pg.MigrationsAssembly(migrationsAssembly);
                        });
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = db => db.UseNpgsql(Configuration.GetConnectionString("IdentityServerContext")!,
                        pg =>
                        {
                            pg.MigrationsAssembly(migrationsAssembly);
                        });
                });

            identity.AddDeveloperSigningCredential(false);
            services.AddSingleton(Configuration);
        }

        public void Configure(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseIdentityServer();
        }
    }
}
