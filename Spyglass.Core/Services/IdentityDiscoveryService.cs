using IdentityModel.Client;
using Newtonsoft.Json;
using Spyglass.Identity;

namespace Spyglass.Core.Services
{
    /// <summary>
    /// Used to cache the IdentityServer discovery endpoint result.
    /// </summary>
    public class IdentityDiscoveryService
    {
        private Task<DiscoveryDocumentResponse>? _discoveryTask = null;
        private DiscoveryDocumentResponse? _discoveryDocument = null;
        private readonly IConfiguration _config;

        public IdentityDiscoveryService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<DiscoveryDocumentResponse> GetDiscoveryDocumentAsync()
        {
            if (_discoveryDocument != null)
            {
                return _discoveryDocument;
            }

            if (_discoveryTask is { IsCompleted: false })
            {
                return await _discoveryTask;
            }

            // Don't need all of this since it'll only be accessible in localhost.
            using var client = new HttpClient();
            _discoveryTask = client.GetDiscoveryDocumentAsync(_config["IntrospectionAuthority"]);
            await _discoveryTask;

            _discoveryDocument = _discoveryTask.Result;
            
            return _discoveryDocument;
        }
    }
}