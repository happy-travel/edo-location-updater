using System;
using HappyTravel.VaultClient;
using Microsoft.Extensions.Configuration;

namespace Common
{
    public static class StartupHelper
    {
        public static VaultClient CreateVaultClient(IConfiguration configuration)
        {
            var vaultOptions = new VaultOptions
            {
                BaseUrl = new Uri(configuration[configuration["Vault:Endpoint"]]),
                Engine = configuration["Vault:Engine"],
                Role = configuration["Vault:Role"]
            };
            
            return new VaultClient(vaultOptions);
        }
        
        
        public static string GetDbConnectionString(VaultClient vaultClient, IConfiguration configuration)
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