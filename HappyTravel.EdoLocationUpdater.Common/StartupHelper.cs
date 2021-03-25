using System;
using HappyTravel.VaultClient;
using Microsoft.Extensions.Configuration;

namespace HappyTravel.EdoLocationUpdater.Common
{
    public static class StartupHelper
    {
        public static VaultClient.VaultClient CreateVaultClient(IConfiguration configuration)
        {
            var vaultOptions = new VaultOptions
            {
                BaseUrl = new Uri(configuration[configuration["Vault:Endpoint"]]),
                Engine = configuration["Vault:Engine"],
                Role = configuration["Vault:Role"]
            };
            
            return new VaultClient.VaultClient(vaultOptions);
        }
        
        
        public static string GetDbConnectionString(VaultClient.VaultClient vaultClient, IConfiguration configuration)
        {
            var connectionOptions = vaultClient.Get(configuration["Database:ConnectionOptions"]).Result;
            
            return string.Format(configuration["Database:ConnectionString"],
                connectionOptions["host"],
                connectionOptions["port"],
                connectionOptions["userId"],
                connectionOptions["password"]);
        }
    }
}