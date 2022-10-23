﻿using IdentityModel.Client;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Core;
using Spyglass.Core.Services;
using Spyglass.Identity;
using Spyglass.Models;
using Spyglass.Models.Admin;

namespace SpyglassNET.Controllers
{
    [ApiController]
    [RequireHttps]
    [Authorize("admin")]
    [Route("admin/")]
    public class AdminController : Controller
    {
        private readonly ConfigurationDbContext _identityConfig;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IdentityDiscoveryService _discovery;
        private readonly PersistedGrantDbContext _persistedGrant;
        private readonly Logger _log;

        public AdminController(ConfigurationDbContext identityConfig, IHttpClientFactory httpFactory, IdentityDiscoveryService discovery, Logger log, PersistedGrantDbContext persistedGrant)
        {
            _identityConfig = identityConfig;
            _httpFactory = httpFactory;
            _discovery = discovery;
            _log = log;
            _persistedGrant = persistedGrant;
        }

        [HttpGet]
        [Route("auth_with_server")]
        public IActionResult AuthWithServer()
        {
            return Ok("Hello, world!");
        }

        /// <summary>
        /// Creates a client with the given id, secret and scopes.
        /// </summary>
        /// <param name="clientId"> The client id to give to the new client. </param>
        /// <param name="clientSecret"> The secret to give to the client. </param>
        /// <param name="scopes"> Scopes to give to the client. </param>
        /// <returns> An ApiResult containing 'Success', and 'Error' on failure. </returns>
        [HttpPost]
        [Route("add_client")]
        public async Task<IActionResult> AddClient(string clientId, string clientSecret, string scopes)
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

            if (string.IsNullOrWhiteSpace(scopes))
            {
                return Ok(new ApiResult
                {
                    Success = false,
                    Error = "Cannot add a client with no valid scopes."
                });
            }

            var validScopes = scopes.Split(" ")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            clientId = clientId.Trim();

            // Can't have a client with the same client id.
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
                    new Secret(clientSecret.Sha256())
                },
                AccessTokenType = AccessTokenType.Reference,
                AccessTokenLifetime = int.MaxValue,
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = validScopes
            };

            _identityConfig.Clients.Add(client.ToEntity());
            await _identityConfig.SaveChangesAsync();

            _log.Information("Created new client \'{ClientId}\' with scopes \'{Scopes}\'", clientId, scopes);
            return Ok(new ApiResult
            {
                Success = true,
            });
        }

        [HttpPost]
        [Route("delete_client")]
        public async Task<IActionResult> DeleteClient(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Ok(new TokenRequestResult
                {
                    Success = false,
                    Error = "Cannot delete client with invalid 'clientId' parameter."
                });
            }

            if (clientId == AuthorizationConfig.SpyglassAdminClientId)
            {
                return Ok(new TokenRequestResult
                {
                    Success = false,
                    Error = "Cannot delete the Spyglass admin client."
                });
            }

            var foundClient = _identityConfig.Clients.FirstOrDefault(c => c.ClientId == clientId);
            if (foundClient == null)
            {
                return Ok(new ApiResult
                {
                    Success = false,
                    Error = $"Cannot delete unknown client '{clientId}'."
                });
            }
            
            _identityConfig.Clients.Remove(foundClient);
            await _identityConfig.SaveChangesAsync();
            
            _log.Information("Deleted client {ClientId}", clientId);
            return Ok(ApiResult.FromSuccess());
        }

        /// <summary>
        /// Requests a new token for the Spyglass admin client.
        /// </summary>
        /// <param name="clientSecret"> The secret for the Spyglass admin client. </param>
        [HttpGet]
        [AllowAnonymous]
        [Route("request_admin_token")]
        public async Task<IActionResult> RequestAdminToken(string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                return Ok(new TokenRequestResult
                {
                    Success = false,
                    Error = "Cannot request token with invalid 'clientSecret' parameter."
                });
            }

            var clientId = AuthorizationConfig.SpyglassAdminClientId;
            var discovery = await _discovery.GetDiscoveryDocumentAsync();
            var client = _httpFactory.CreateClient();

            var currentGrants = _persistedGrant.PersistedGrants.Where(g => g.ClientId == clientId).ToList();

            _log.Information("Requesting a new token for client {ClientId}", clientId);
            var token = await client.RequestTokenAsync(new TokenRequest
            {
                Address = discovery.TokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                GrantType = GrantType.ClientCredentials,
            });

            if (token.IsError)
            {
                _log.Error("Token request failed for client {ClientId} with error: {Error}", clientId, token.Error);
                return Ok(new TokenRequestResult
                {
                    Success = false,
                    Error = $"Failed to request a token for client id '{clientId}': {token.Error}."
                });
            }

            // Make sure we don't have any other tokens for this client id.
            // If so, revoke them.
            _persistedGrant.PersistedGrants.RemoveRange(currentGrants);
            await _persistedGrant.SaveChangesAsync();

            _log.Information("Successfully created new {TokenType} token for client {ClientId} with scopes {Scopes}", token.TokenType, clientId, token.Scope);
            return Ok(new TokenRequestResult
            {
                Success = true,
                Token = token.AccessToken,
                TokenType = token.TokenType,
                Scope = token.Scope
            });
        }

        [HttpGet]
        [Route("request_token")]
        public async Task<IActionResult> RequestToken(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Ok(new TokenRequestResult
                {
                    Success = false,
                    Error = "Cannot request token with invalid 'clientId' parameter."
                });
            }

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                return Ok(new TokenRequestResult
                {
                    Success = false,
                    Error = "Cannot request token with invalid 'clientSecret' parameter."
                });
            }

            var discovery = await _discovery.GetDiscoveryDocumentAsync();
            var client = _httpFactory.CreateClient();

            var currentGrants = _persistedGrant.PersistedGrants.Where(g => g.ClientId == clientId).ToList();

            _log.Information("Requesting a new token for client {ClientId}", clientId);
            var token = await client.RequestTokenAsync(new TokenRequest
            {
                Address = discovery.TokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                GrantType = GrantType.ClientCredentials,
            });

            if (token.IsError)
            {
                _log.Error("Token request failed for client {ClientId} with error: {Error}", clientId, token.Error);
                return Ok(new TokenRequestResult
                {
                    Success = false,
                    Error = $"Failed to request a token for client id '{clientId}': {token.Error}."
                });
            }

            // Make sure we don't have any other tokens for this client id.
            // If so, revoke them.
            _persistedGrant.PersistedGrants.RemoveRange(currentGrants);
            await _persistedGrant.SaveChangesAsync();

            _log.Information("Successfully created new {TokenType} token for client {ClientId} with scopes {Scopes}", token.TokenType, clientId, token.Scope);
            return Ok(new TokenRequestResult
            {
                Success = true,
                Token = token.AccessToken,
                TokenType = token.TokenType,
                Scope = token.Scope
            });
        }

        [HttpGet]
        [Route("revoke_token")]
        public async Task<IActionResult> RevokeTokens(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Ok(new ApiResult
                {
                    Success = false,
                    Error = "Cannot revoke token with invalid 'clientId' parameter."
                });
            }

            var currentGrants = _persistedGrant.PersistedGrants.Where(g => g.ClientId == clientId).ToList();
            if (currentGrants.Any())
            {
                _persistedGrant.PersistedGrants.RemoveRange(currentGrants);
                await _persistedGrant.SaveChangesAsync();

                _log.Information("Revoked access token for client {ClientId}", clientId);
                return Ok(ApiResult.FromSuccess());
            }
            
            return Ok(new ApiResult
            {
                Success = false,
                Error = $"Client '{clientId}' has no access token to revoke."
            });
        }
    }
}