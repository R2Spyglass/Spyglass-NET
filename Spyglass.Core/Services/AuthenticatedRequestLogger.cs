using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Spyglass.Core.Database;
using Spyglass.Models.Admin;
using ILogger = Serilog.ILogger;

namespace Spyglass.Core.Services
{
    public class AuthenticatedRequestLogger
    {
        private readonly ILogger _log;
        private readonly IServiceScopeFactory _scopeFactory;
        
        public AuthenticatedRequestLogger(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _log = Log.Logger;
        }

        /// <summary>
        /// Called by middleware when an api request is received.
        /// Will log it if authenticated.
        /// </summary>
        /// <param name="context"> The context of the api request. </param>
        public async Task OnRequestReceivedAsync(HttpContext context)
        {
            // Only log authenticated requests.
            if (context.User.Identity is null or { IsAuthenticated: false })
            {
                return;
            }
            
            // Make sure there's a client-id claim, and a remote ip address.
            if (!context.User.HasClaim(c => c.Type == "client_id")
                || context.Connection.RemoteIpAddress == null)
            {
                return;
            }
            
            using var scope = _scopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<SpyglassContext>();

            var clientId = context.User.FindFirstValue("client_id")!;
            var address = context.Connection.RemoteIpAddress.ToString();

            if (context.Request.Headers.ContainsKey("CF-Connecting-IP"))
            {
                address = context.Request.Headers["CF-Connecting-IP"][0]!;
            }
            
            var serverName = context.Request.Headers.ContainsKey("Northstar-Server-Name") ? context.Request.Headers["Northstar-Server-Name"][0] : null;

            var exists = dbContext.AuthenticatedRequests.AsNoTracking()
                .Any(a => a.ClientId == clientId && a.IpAddress == address);

            if (exists)
            {
                return;
            }

            var requestData = new AuthenticatedRequestData
            {
                ClientId = clientId,
                IpAddress = address,
                RequestTime = DateTimeOffset.UtcNow,
                ServerName = serverName
            };

            try
            {
                dbContext.AuthenticatedRequests.Add(requestData);
                await dbContext.SaveChangesAsync();
                _log.Information("Logged new authenticated request ip address for client_id '{ClientId}'", clientId);
            }
            catch (Exception e)
            {
                _log.Error(e, "An error has occurred while logging new authenticated request for client_id '{ClientId}'", clientId);
            }
        }
    }
}

