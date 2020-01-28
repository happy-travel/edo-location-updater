using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HappyTravel.LocationUpdater.Infrastructure
{
    public static class LocalizationHelper
    {
        public static string GetDefaultFromLocalizedName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            
                var localizedName = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
                localizedName.TryGetValue(DefaultLanguageCode, out var defaultName);
                return defaultName;
        }

        private const string DefaultLanguageCode = "en";
    }
}