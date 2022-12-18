using IdentityServer4.Models;

namespace Spyglass.Identity
{
    /// <summary>
    /// Constants for Spyglass' api scopes.
    /// </summary>
    public static class ApiScopes
    {
        public static readonly string Admin = "admin";
        public static readonly string Maintainer = "maintainer";
        public static readonly string TrustedServer = "trusted_server";
        public static readonly string Players = "players";
        public static readonly string Sanctions = "sanctions";
    }
    
    public static class AuthorizationConfig
    {
        public static IEnumerable<ApiScope> Scopes => new List<ApiScope>
        {
            new ApiScope(ApiScopes.Admin, "Administration Scope"),
            new ApiScope(ApiScopes.Maintainer, "Maintainer Scope"),
            new ApiScope(ApiScopes.TrustedServer, "Trusted Server Scope"),
            new ApiScope(ApiScopes.Players, "Players Scope"),
            new ApiScope(ApiScopes.Sanctions, "Sanctions Scope")
        };
        
        public static IEnumerable<ApiResource> ApiResources(IConfiguration? config) => new List<ApiResource>
        {
            new ApiResource("privileged")
            {
                Scopes = AuthorizationConfig.Scopes.Select(s => s.Name).ToList(),
                ApiSecrets = config != null 
                    ? new List<Secret> {  new Secret(config["IntrospectionApiSecret"].Sha256()) }
                    : new List<Secret>()
            }
        };

        /// <summary>
        /// The url to start the identity server on.
        /// </summary>
        public static readonly string IdentityServerUrl = "https://localhost:5001";
        
        /// <summary>
        /// The client id used for Spyglass administration.
        /// </summary>
        public static readonly string SpyglassAdminClientId = "spyglass-admin";
    }
}