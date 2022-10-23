using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Spyglass.Core.Database;
using Spyglass.Core.Services;
using Spyglass.Identity;
using Spyglass.Models;

namespace Spyglass.Core
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        
        public Startup(IConfigurationRoot configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddAuthentication("token")
                .AddOAuth2Introspection("token", options =>
                {
                    options.Authority = AuthorizationConfig.IdentityServerUrl;
                    options.ClientId = "privileged";
                    options.ClientSecret = Configuration["IntrospectionApiSecret"];
                });

            services.AddAuthorization(options =>
            {
                var scopes = AuthorizationConfig.ApiScopes
                    .Select(s => s.Name)
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                
                foreach (var scope in scopes)
                {
                    options.AddPolicy(scope!, policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireClaim("scope", scope!);
                    });
                }
            });

            // Setup API endpoint controllers.
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
                });
            
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            
            // Setup Spyglass' efcore database context.
            services.AddDbContext<SpyglassContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("SpyglassContext"));
                options.UseSnakeCaseNamingConvention();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
            });

            services.AddSingleton(Configuration);
            services.AddSingleton<IdentityDiscoveryService>();
        }

        public void Configure(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler(handler =>
                {
                    handler.Run(async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = MediaTypeNames.Application.Json;

                        var result = new ApiResult
                        {
                            Success = false,
                            Error = "An internal server error has occurred while attempting to fulfill your request. Please try again later."
                        };

                        var json = JsonConvert.SerializeObject(result, new JsonSerializerSettings
                        {
                            ContractResolver = new DefaultContractResolver
                            {
                                NamingStrategy = new CamelCaseNamingStrategy()
                            }
                        });

                        await context.Response.WriteAsync(json);
                    });
                });
            }
            // Redirect HTTP requests to HTTPS.
            app.UseHttpsRedirection();
            
            // Authenticate and authorize using IdentityServer4.
            app.UseAuthentication();
            app.UseAuthorization();
            
            // Map controllers to their endpoints, and add a fallback for a not found page (or any other non-handled requests).
            app.MapControllers();
            app.UseStatusCodePages(async context =>
            {
                context.HttpContext.Response.ContentType = MediaTypeNames.Application.Json;

                var readableCode = Regex.Replace(((HttpStatusCode) context.HttpContext.Response.StatusCode).ToString(), "(\\B[A-Z])", " $1");
                var result = new ApiResult
                {
                    Success = false,
                    Error = $"{context.HttpContext.Response.StatusCode}: {readableCode}"
                };

                var json = JsonConvert.SerializeObject(result, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    }
                });
                
                await context.HttpContext.Response.WriteAsync(json);
            });
            
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Spyglass-API-Version", Configuration["SpyglassVersion"]);
                context.Response.Headers.Add("Spyglass-API-MinimumVersion", Configuration["SpyglassMinimumVersion"]);
                await next.Invoke();
            });
        }
    }
}
