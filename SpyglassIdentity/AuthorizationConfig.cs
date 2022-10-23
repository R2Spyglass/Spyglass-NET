using IdentityServer4.Models;

namespace Spyglass.Identity
{
    public static class AuthorizationConfig
    {
        public static IEnumerable<ApiScope> ApiScopes => new List<ApiScope>
        {
            new ApiScope("admin", "Administration Scope"),
            new ApiScope("trusted_server", "Trusted Server Scope"),
            new ApiScope("sanctions", "Sanctions Scope")
        };
        
        public static IEnumerable<ApiResource> ApiResources(IConfiguration? config) => new List<ApiResource>
        {
            new ApiResource("privileged")
            {
                Scopes = new List<string>
                {
                    "admin", "trusted_server", "sanctions"
                },
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