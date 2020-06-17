using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using HappyTravel.Data;
using HappyTravel.LocationUpdater.Infrastructure;
using HappyTravel.LocationUpdater.Services;
using IdentityModel.Client;
using LocationNameNormalizer.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
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
            Dictionary<string, string> dataProviderPaths = null;

            using var vaultClient = StartupHelper.CreateVaultClient(Configuration);

            vaultClient.Login(GetFromEnvironment("Vault:Token")).Wait();

            var jobsSettings = vaultClient.Get(Configuration["Identity:JobsOptions"]).Result;
            clientSecret = jobsSettings[Configuration["Identity:Secret"]];

            var edoSettings = vaultClient.Get(Configuration["Edo:EdoOptions"]).Result;
            authorityUrl = edoSettings[Configuration["Identity:Authority"]];
            edoApiUrl = edoSettings[Configuration["Edo:Api"]];

            dataProviderPaths = vaultClient.Get(Configuration["DataProviders:Paths"]).Result
                .Where(i => i.Key != "enabledConnectors")
                .ToDictionary(i => i.Key, j => j.Value);

            var updaterOptions = vaultClient.Get(Configuration["DataProviders:Options"]).Result;
            var enabledProviders = updaterOptions["enabled"].Split(';').Select(i => i.Trim());

            var connectionString = StartupHelper.GetDbConnectionString(vaultClient, Configuration);
            services.AddDbContext<LocationUpdaterContext>(options =>
            {
                options.UseNpgsql(
                    connectionString,
                    b => b.UseNetTopologySuite());
                options.EnableSensitiveDataLogging(false);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            services.AddTransient<ProtectedApiBearerTokenHandler>();
            services.AddTransient<ConnectorsBearerTokenHandler>();
            services.AddTransient<JsonSerializer>();

            services.Configure<TokenRequest>(options =>
            {
                var uri = new Uri(new Uri(authorityUrl), "/connect/token");
                options.Address = uri.ToString();
                options.ClientId = Configuration["Identity:ClientId"];
                options.ClientSecret = clientSecret;
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

            AddDataProvidersHttpClients(services, dataProviderPaths, enabledProviders);

            services.Configure<UpdaterOptions>(o =>
            {
                var batchSizeSetting = Environment.GetEnvironmentVariable("BATCH_SIZE");

                o.BatchSize = int.TryParse(batchSizeSetting, out var batchSize)
                    ? batchSize
                    : 2000;

                var requestDelaySetting = Environment.GetEnvironmentVariable("REQUEST_DELAY");
                o.UploadRequestDelay = int.TryParse(requestDelaySetting, out var requestDelayMilliseconds)
                    ? TimeSpan.FromMilliseconds(requestDelayMilliseconds)
                    : TimeSpan.FromMilliseconds(150);

                o.DataProviders = enabledProviders;
            });

            services.AddHostedService<LocationUpdaterHostedService>();
            services.AddNameNormalizationServices();
            services.AddHttpClient();
            services.AddHealthChecks();
        }


        private IServiceCollection AddDataProvidersHttpClients(IServiceCollection services,
            Dictionary<string, string> dataProviderPaths, IEnumerable<string> enabledConnectors)
        {
            foreach (var (providerKey, basePath) in dataProviderPaths.Where(kv => enabledConnectors.Contains(kv.Key)))
            {
                services.AddHttpClient(providerKey, client =>
                    {
                        client.BaseAddress = client.BaseAddress = new Uri(basePath);
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.Timeout = TimeSpan.FromMinutes(10);
                    }).SetHandlerLifetime(TimeSpan.FromMinutes(10))
                    .AddPolicyHandler(HttpClientPolicies.GetRetryPolicy())
                    .AddHttpMessageHandler<ConnectorsBearerTokenHandler>();
            }

            return services;
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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