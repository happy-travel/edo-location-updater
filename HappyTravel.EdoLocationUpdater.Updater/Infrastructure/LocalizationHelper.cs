using System.Collections.Generic;
using Newtonsoft.Json;

namespace HappyTravel.EdoLocationUpdater.Updater.Infrastructure
{
    public static class LocalizationHelper
    {
        public static string GetDefaultFromLocalizedName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var localizedName = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            localizedName.TryGetValue(DefaultLanguageCode, out var defaultName);
            return defaultName;
        }

        private const string DefaultLanguageCode = "en";
    }
}