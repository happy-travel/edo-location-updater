using System;
using System.Collections.Generic;
using HappyTravel.LocationUpdater.Infrastructure;
using HappyTravel.LocationUpdater.Services;
using HappyTravel.VaultClient;
using IdentityModel.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HappyTravel.LocationUpdater
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var serializationSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None
            };
            JsonConvert.DefaultSettings = () => serializationSettings;

            string clientSecret;
            string authorityUrl;
            string edoApiUrl;
            Dictionary<string, string> dataProvidersOptions = null;

            using (var vaultClient = new VaultClient.VaultClient(new VaultOptions
            {
                Role = Configuration["Vault:Role"],
                BaseUrl = new Uri(GetFromEnvironment("Vault:Endpoint")),
                Engine = Configuration["Vault:Engine"]
            }, null))
            {
                vaultClient.Login(GetFromEnvironment("Vault:Token")).Wait();

                var jobsSettings = vaultClient.Get(Configuration["Identity:JobsOptions"]).Result;
                clientSecret = jobsSettings[Configuration["Identity:Secret"]];

                var edoSettings = vaultClient.Get(Configuration["Edo:EdoOptions"]).Result;
                authorityUrl = edoSettings[Configuration["Identity:Authority"]];
                edoApiUrl = edoSettings[Configuration["Edo:Api"]];
                dataProvidersOptions = vaultClient.Get(Configuration["DataProviders:Options"]).Result;
            }

            services.AddTransient<ProtectedApiBearerTokenHandler>();
            services.AddTransient<JsonSerializer>();
            services.Configure<ClientCredentialsTokenRequest>(options =>
            {
                var uri = new Uri(new Uri(authorityUrl), "/connect/token");
                options.Address = uri.ToString();
                options.ClientId = Configuration["Identity:ClientId"];
                options.ClientSecret = clientSecret;
                options.Scope = "edo";
            });

            services.AddHttpClient(HttpClientNames.Identity, client =>
            {
                client.BaseAddress = new Uri(authorityUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            services.AddHttpClient(HttpClientNames.EdoApi, client =>
                {
                    client.BaseAddress = new Uri(edoApiUrl);
                    client.Timeout = TimeSpan.FromMinutes(5);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddHttpMessageHandler<ProtectedApiBearerTokenHandler>();

            services.AddHttpClient(HttpClientNames.NetstormingConnector, client =>
                {
                    client.BaseAddress = new Uri(dataProvidersOptions["netstormingConnector"]);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.Timeout = TimeSpan.FromMinutes(10);
                }).SetHandlerLifetime(TimeSpan.FromMinutes(10))
                .AddPolicyHandler(HttpClientPolicies.GetRetryPolicy());

            services.AddHttpClient(HttpClientNames.Illusions, client =>
                {
                    client.BaseAddress = new Uri(dataProvidersOptions[HttpClientNames.Illusions]);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.Timeout = TimeSpan.FromMinutes(10);
                }).SetHandlerLifetime(TimeSpan.FromMinutes(10))
                .AddPolicyHandler(HttpClientPolicies.GetRetryPolicy());

            services.Configure<UpdaterOptions>(o =>
            {
                var batchSizeSetting = Environment.GetEnvironmentVariable("BatchSize");

                o.BatchSize = int.TryParse(batchSizeSetting, out var batchSize)
                    ? batchSize
                    : 100;

                var requestDelaySetting = Environment.GetEnvironmentVariable("RequestDelay");
                o.UploadRequestDelay = int.TryParse(requestDelaySetting, out var requestDelayMilliseconds)
                    ? TimeSpan.FromMilliseconds(requestDelayMilliseconds)
                    : TimeSpan.FromMilliseconds(50);
            });

            services.AddHttpClient();
            services.AddHealthChecks();
            services.AddHostedService<LocationUpdaterHostedService>();
        }


        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHealthChecks("/health");
        }


        private string GetFromEnvironment(string key)
        {
            var environmentVariable = Configuration[key];
            if (environmentVariable is null)
                throw new Exception($"Couldn't obtain the value for '{key}' configuration key.");

            return Environment.GetEnvironmentVariable(environmentVariable);
        }

        public IConfiguration Configuration { get; }
    }
}