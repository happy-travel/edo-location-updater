using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace HappyTravel.LocationUpdater.Infrastructure
{
    public class ProtectedApiBearerTokenHandler : DelegatingHandler
    {
        public ProtectedApiBearerTokenHandler(TokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }


        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.SetBearerToken(await _tokenProvider.GetToken());
            return await base.SendAsync(request, cancellationToken);
        }
        
        private readonly TokenProvider _tokenProvider;
    }
}