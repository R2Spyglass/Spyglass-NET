using IdentityModel.Client;
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

        public IdentityDiscoveryService()
        {
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

            using var client = new HttpClient();
            _discoveryTask = client.GetDiscoveryDocumentAsync(AuthorizationConfig.IdentityServerUrl);
            await _discoveryTask;

            _discoveryDocument = _discoveryTask.Result;
            return _discoveryDocument;
        }
    }
}