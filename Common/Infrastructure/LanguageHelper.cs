using System.Collections.Generic;
using Newtonsoft.Json;

namespace Common.Infrastructure
{
    public static class LanguageHelper
    {
        public static string GetValue(string source, string languageCode)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(source);

            return !jsonDictionary.TryGetValue(languageCode, out var languageValue)
                ? string.Empty
                : languageValue;
        }
        
        
        public static string CombineLanguages(string firstJsonWithLanguages, string secondJsonWithLanguages)
        {
            if (string.IsNullOrEmpty(firstJsonWithLanguages))
                return secondJsonWithLanguages;

            if (string.IsNullOrEmpty(secondJsonWithLanguages))
                return firstJsonWithLanguages;
                
            var firstJsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(firstJsonWithLanguages);
            var secondJsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(secondJsonWithLanguages);

            Dictionary<string, string> largerJsonObject;
            Dictionary<string, string> smallerJsonObject;

            if (firstJsonObject.Count > secondJsonObject.Count)
            {
                largerJsonObject = firstJsonObject;
                smallerJsonObject = secondJsonObject;
            }
            else
            {
                largerJsonObject = secondJsonObject;
                smallerJsonObject = firstJsonObject;;
            }

            foreach (var languageCandidate in smallerJsonObject)
            {
                if (!largerJsonObject.ContainsKey(languageCandidate.Key))
                {
                    largerJsonObject.Add(languageCandidate.Key, languageCandidate.Value);
                }
            }
            
            return JsonConvert.SerializeObject(largerJsonObject);
        }
    }
}