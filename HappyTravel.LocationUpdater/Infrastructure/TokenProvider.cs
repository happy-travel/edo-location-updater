using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyTravel.LocationUpdater.Infrastructure
{
    public class TokenProvider
    {
        public TokenProvider(IHttpClientFactory clientFactory,
            IOptions<TokenRequestSettings> options,
            ILogger<TokenProvider> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _options = options.Value;
        }
        
        public async ValueTask<string> GetToken()
        {
            await _tokenSemaphore.WaitAsync(); 
            var dateNow = DateTime.UtcNow;
            // We need to cache token because users can send several requests in short periods.
            // Covered situation when after checking expireDate token will expire immediately.
            if (_options.IsCachingDisabled || _tokenInfo.Equals(default) || (_tokenInfo.ExpiryDate - dateNow).TotalSeconds < 15)
            {
                var tokenResponse = await GetTokenFromIdentity();
                _tokenInfo = (tokenResponse.AccessToken, dateNow.AddSeconds(tokenResponse.ExpiresIn));
            }
            
            _tokenSemaphore.Release();
            return _tokenInfo.Token;
        }

        
        private async Task<TokenResponse> GetTokenFromIdentity()
        {
            using var client = _clientFactory.CreateClient(HttpClientNames.Identity);
            
            _logger.LogDebug($"Getting token from identity server on {_options.TokenRequestUrl}");
            
            var clientCredentialsTokenRequest = new ClientCredentialsTokenRequest
            {
                Address = _options.TokenRequestUrl,
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret,
                Scope = string.Join(" ", _options.Scopes)
            };

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(clientCredentialsTokenRequest);
            if (tokenResponse.IsError)
                throw new HttpRequestException(
                    $"Something went wrong while requesting the access token For Connectors. Error: {tokenResponse.Error}");
            
            _logger.LogDebug($"Fetched access token: {tokenResponse.AccessToken}");

            return tokenResponse;
        }

        private readonly TokenRequestSettings _options;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<TokenProvider> _logger;
        private (string Token, DateTime ExpiryDate) _tokenInfo;
        private readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1, 1);
    }
}