using System;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;

namespace HappyTravel.EdoLocationUpdater.Updater.Infrastructure
{
    internal static class HttpClientPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> GetStandardRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.Forbidden)
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                    retryAttempt)));
        }
    }
}