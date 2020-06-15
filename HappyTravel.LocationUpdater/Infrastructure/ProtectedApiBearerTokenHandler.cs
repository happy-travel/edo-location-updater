using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace HappyTravel.LocationUpdater.Infrastructure
{
    public class ProtectedApiBearerTokenHandler : DelegatingHandler
    {
        public ProtectedApiBearerTokenHandler(IHttpClientFactory clientFactory, IOptions<TokenRequest> tokenRequest)
        {
            _clientFactory = clientFactory;
            _tokenRequest = tokenRequest.Value;
        }


        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.SetBearerToken(await GetToken());
            return await base.SendAsync(request, cancellationToken);
        }


        private async Task<string> GetToken()
        {
            await TokenSemaphore.WaitAsync();
            var dateNow = DateTime.UtcNow;
            // We need to cache token because users can send several requests in short periods.
            // Covered situation when after checking expireDate token will expire immediately.
            if (!_tokenInfo.Equals(default) && (_tokenInfo.ExpiryDate - dateNow).TotalSeconds >= 5)
            {
                TokenSemaphore.Release();
                return _tokenInfo.Token;
            }

            using var client = _clientFactory.CreateClient(HttpClientNames.Identity);
            var clientCredentialsTokenRequest = new ClientCredentialsTokenRequest
            {
                Address = _tokenRequest.Address,
                ClientId = _tokenRequest.ClientId,
                ClientSecret = _tokenRequest.ClientSecret,
                Scope = "edo"
            };

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(clientCredentialsTokenRequest);
            if (tokenResponse.IsError)
                throw new HttpRequestException(
                    $"Something went wrong while requesting the access token For Connectors. Error: {tokenResponse.Error}");

            _tokenInfo = (tokenResponse.AccessToken, dateNow.AddSeconds(tokenResponse.ExpiresIn));
            TokenSemaphore.Release();
            return _tokenInfo.Token;
        }


        private readonly IHttpClientFactory _clientFactory;
        private readonly TokenRequest _tokenRequest;

        private static (string Token, DateTime ExpiryDate) _tokenInfo;
        private static readonly SemaphoreSlim TokenSemaphore = new SemaphoreSlim(1, 1);
    }
}