namespace HappyTravel.EdoLocationUpdater.Updater.Infrastructure
{
    public class TokenRequestSettings
    {
        public string TokenRequestUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string[] Scopes { get; set; }
        public bool IsCachingDisabled { get; set; }
    }
}