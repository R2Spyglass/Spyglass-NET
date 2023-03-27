using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.RegularExpressions;
using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityModel.Client;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
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

        public void ConfigureServices(IWebHostEnvironment environment, IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddAuthentication("token")
                .AddOAuth2Introspection("token", options =>
                {
                    options.Authority = Configuration["IntrospectionAuthority"];
                    options.ClientId = "privileged";
                    options.ClientSecret = Configuration["IntrospectionApiSecret"];
                    options.DiscoveryPolicy = new DiscoveryPolicy
                    {
                        RequireKeySet = false,
                        RequireHttps = false,
                        AllowHttpOnLoopback = true,
                    };
                });

            services.AddAuthorization(options =>
            {
                var scopes = AuthorizationConfig.Scopes
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
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    };
                });
            
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddHttpContextAccessor();
            
            // Setup Spyglass' efcore database context.
            services.AddDbContext<SpyglassContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("SpyglassContext")!);
                options.UseSnakeCaseNamingConvention();
            });
            
            // Setup IdentityServer4 database contexts.
            services.AddSingleton<ConfigurationStoreOptions>();
            services.AddSingleton<OperationalStoreOptions>();
            services.AddDbContext<ConfigurationDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("IdentityServerContext")!);
            });
            
            services.AddDbContext<PersistedGrantDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("IdentityServerContext")!);
            });

            services.AddSingleton(Configuration)
                .AddSingleton<IdentityDiscoveryService>()
                .AddSingleton<MaintainerAuthenticationService>()
                .AddSingleton<AuthenticatedRequestLogger>();

            // if (environment.IsProduction())
            // {
            //     services.AddLettuceEncrypt();
            // }
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
            
            // Authenticate and authorize using IdentityServer4.
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSerilogRequestLogging();
            
            // Override request remote ip address when forwarded.
            app.UseForwardedHeaders(new ForwardedHeadersOptions()
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                ForwardedForHeaderName = "CF-Connecting-IP"
            });

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

                var requestLogger = app.Services.GetRequiredService<AuthenticatedRequestLogger>();
                await requestLogger.OnRequestReceivedAsync(context);
                
                await next.Invoke();
            });
        }
    }
}
