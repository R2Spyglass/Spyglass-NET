using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spyglass.Core.Services;
using Spyglass.Models;

namespace SpyglassNET.Controllers
{
    [ApiController]
    [Authorize("admin")]
    [Route("admin/")]
    public class AdminController : Controller
    {
        private readonly ConfigurationDbContext _identityConfig;
        private readonly IdentityDiscoveryService _discovery;
        private readonly IPersistedGrantService _persistedGrant;

        public AdminController(ConfigurationDbContext identityConfig, IdentityDiscoveryService discovery, IPersistedGrantService persistedGrant)
        {
            _identityConfig = identityConfig;
            _discovery = discovery;
            _persistedGrant = persistedGrant;
        }
        
        [HttpGet]
        [Route("auth_with_server")]
        public IActionResult AuthWithServer()
        {
            return Ok("Hello, world!");
        }

        [HttpPost]
        [Route("add_client")]
        public async Task<IActionResult> AddClient(string clientId, string?[] scopes)
        {
            // Make sure we only create a client with a valid client id and scopes.
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Ok(new ApiResult
                {
                    Success = false,
                    Error = "Cannot add a client with null or whitespace parameter 'clientId'."
                });
            }
            
            var validScopes = scopes
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .ToList();

            if (!scopes.Any())
            {
                return Ok(new ApiResult
                {
                    Success = false,
                    Error = "Cannot add a client with no valid scopes."
                });
            }

            clientId = clientId.Trim();

            if (_identityConfig.Clients.Any(c => c.ClientId == clientId))
            {
                return Ok(new ApiResult
                {
                    Success = false,
                    Error = $"A client with id '{clientId}' already exists."
                });
            }

            var client = new Client
            {
                ClientId = clientId,
                ClientSecrets =
                {
                    new Secret(Convert.ToBase64String(CryptoRandom.CreateRandomKey(32)).Sha256())
                },
                AccessTokenType = AccessTokenType.Reference,
                AccessTokenLifetime = int.MaxValue,
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = validScopes
            };

            _identityConfig.Clients.Add(client.ToEntity());
            await _identityConfig.SaveChangesAsync();
            
            return Ok(new ApiResult
            {
                Success = true,
            });
        }
    }
}

