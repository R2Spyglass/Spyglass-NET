using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Spyglass.Core.Database;
using Spyglass.Models;
using Spyglass.Models.Admin;
using SpyglassNET.Utilities;
using ILogger = Serilog.ILogger;

namespace Spyglass.Core.Services
{
    public class MaintainerAuthenticationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _log;
        
        // Current authentication tickets, mapped to a uid.
        private Dictionary<string, MaintainerAuthenticationTicket> _tickets = new();
    
        public MaintainerAuthenticationService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _log = Log.Logger;
        }

        /// <summary>
        /// Add a maintainer identity to the database.
        /// </summary>
        /// <param name="clientId"> The client id of the maintainer this identity belongs to. </param>
        /// <param name="uniqueId"> The unique id of the account that belongs to the maintainer. </param>
        /// <returns> Whether or not the identity was successfully added. </returns>
        public async Task<ApiResult> AddIdentityAsync(string clientId, string uniqueId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return ApiResult.FromError("Cannot add a maintainer identity with a null or empty client id.");
            }
            
            if (!SpyglassUtils.ValidateUniqueId(uniqueId))
            {
                return ApiResult.FromError("Cannot add a maintainer identity with an invalid unique id.");
            }
            
            clientId = clientId.Trim();
            uniqueId = uniqueId.Trim();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<SpyglassContext>();
            
                var identity = dbContext.MaintainerIdentities
                    .AsNoTracking()
                    .FirstOrDefault(i => i.UniqueId == uniqueId);

                if (identity != null)
                {
                    return ApiResult.FromError("Cannot add a maintainer identity: one already exists for this unique id.");
                }
                
                dbContext.MaintainerIdentities
                    .Add(new MaintainerIdentity {ClientId = clientId, UniqueId = uniqueId});

                await dbContext.SaveChangesAsync();
                _log.Information("Added identity \'{UniqueId}\' to maintainer \'{ClientId}\'", uniqueId, clientId);
                return ApiResult.FromSuccess();
            }
            catch (Exception e)
            {
                return ApiResult.FromError($"An error has occured while creating the identity: {e.Message}");
            }
        }

        /// <summary>
        /// Removes a maintainer identity from the database if it exists.
        /// </summary>
        /// <param name="uniqueId"> The unique id of the account to remove. </param>
        /// <returns></returns>
        public async Task<ApiResult> RemoveIdentityAsync(string uniqueId)
        {
            if (!SpyglassUtils.ValidateUniqueId(uniqueId))
            {
                return ApiResult.FromError("Cannot remove a maintainer identity with an invalid unique id.");
            }

            uniqueId = uniqueId.Trim();
            
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<SpyglassContext>();

                var identity = dbContext.MaintainerIdentities.FirstOrDefault(i => i.UniqueId == uniqueId);

                if (identity == null)
                {
                    return ApiResult.FromError($"Cannot remove unknown maintainer identity '{uniqueId}'");
                }

                dbContext.MaintainerIdentities.Remove(identity);
                await dbContext.SaveChangesAsync();
                _log.Information("Removed identity \'{UniqueId}\' from maintainer \'{ClientId}\'", identity.UniqueId, identity.ClientId);
                
                return ApiResult.FromSuccess();
            }
            catch (Exception e)
            {
                return ApiResult.FromError($"An error has occured while removing the identity: {e.Message}");
            }
        }

        /// <summary>
        /// Authenticates a maintainer if they have an identity, and returns a temporary ticket.
        /// The ticket expires after a short while, so they should get authenticated as soon as possible.
        /// </summary>
        /// <param name="clientId"> The client id of the maintainer attempting to authenticate. </param>
        /// <param name="uniqueId"> The unique id of the account they are authenticating on. </param>
        /// <returns></returns>
        public MaintainerAuthenticationResult CreateAuthenticationTicket(string clientId, string uniqueId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return new MaintainerAuthenticationResult
                {
                    Success = false,
                    Error = "Cannot create an authentication ticket with a null or empty client id."
                };
            }
            
            if (!SpyglassUtils.ValidateUniqueId(uniqueId))
            {
                return new MaintainerAuthenticationResult
                {
                    Success = false,
                    Error = "Cannot create an authentication ticket with an invalid unique id."
                };
            }

            clientId = clientId.Trim();
            uniqueId = uniqueId.Trim();
            
            using var scope = _scopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<SpyglassContext>();

            var identity = dbContext.MaintainerIdentities
                .AsNoTracking()
                .FirstOrDefault(i => i.ClientId == clientId && i.UniqueId == uniqueId);

            if (identity == null)
            {
                return new MaintainerAuthenticationResult
                {
                    Success = false,
                    Error = $"Maintainer does not have any identity with unique id '{uniqueId}'."
                };
            }
            
            // Kill the current authentication ticket if they have one.
            if (_tickets.ContainsKey(uniqueId))
            {
                _tickets.Remove(uniqueId);
            }
            
            // Don't need much security here, expires fast, requires a client credentials token to execute this as well.
            var ticket = new MaintainerAuthenticationTicket
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
                Expiry = DateTimeOffset.Now + TimeSpan.FromMinutes(3)
            };
            
            _tickets.Add(uniqueId, ticket);

            return new MaintainerAuthenticationResult
            {
                Success = true,
                Ticket = ticket
            };
        }

        /// <summary>
        /// Validates an authentication ticket for the given unique id of the authenticating maintainer.
        /// </summary>
        /// <param name="uniqueId"> The unique id of the maintainer attempting to authenticate themselves. </param>
        /// <param name="token"> The token the maintainer attempted to authenticate with. </param>
        /// <param name="consumeTicket"> Whether or not to consume the ticket after validating it. </param>
        /// <returns> Whether or not the authentication ticket is valid. </returns>
        public MaintainerTicketValidationResult ValidateAuthenticationTicket(string uniqueId, string token, bool consumeTicket = true)
        {
            if (!string.IsNullOrWhiteSpace(uniqueId) && _tickets.ContainsKey(uniqueId) && !string.IsNullOrWhiteSpace(token))
            {
                MaintainerAuthenticationTicket ticket = _tickets[uniqueId];
                if (ticket.Token == token && ticket.Expiry >= DateTimeOffset.Now)
                {
                    if (consumeTicket)
                    {
                        _tickets.Remove(uniqueId);
                    }

                    return new MaintainerTicketValidationResult()
                    {
                        Success = true,
                        IsValid = true
                    };
                }
            }

            return new MaintainerTicketValidationResult
            {
                Success = true,
                IsValid = false,
            };
        }
    }
}

