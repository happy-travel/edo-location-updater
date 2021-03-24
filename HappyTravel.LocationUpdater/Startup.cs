using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.EdoLocationUpdater.Common;
using HappyTravel.Data;
using HappyTravel.LocationUpdater.Infrastructure;
using HappyTravel.LocationUpdater.Services;
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

            using var vaultClient = StartupHelper.CreateVaultClient(Configuration);

            vaultClient.Login(GetFromEnvironment("Vault:Token")).Wait();

            var jobsSettings = vaultClient.Get(Configuration["Identity:JobsOptions"]).Result;
            var clientSecret = jobsSettings[Configuration["Identity:Secret"]];

            var edoSettings = vaultClient.Get(Configuration["Edo:EdoOptions"]).Result;
            var authorityUrl = edoSettings[Configuration["Identity:Authority"]];
            var edoApiUrl = edoSettings[Configuration["Edo:Api"]];

            var supplierPaths = vaultClient.Get(Configuration["Suppliers:Paths"]).Result
                .Where(i => i.Key != "enabledConnectors")
                .ToDictionary(i => i.Key, j => j.Value);

            IEnumerable<string> enabledSuppliers;
            var supplierSettingsFromEnvironment = Environment.GetEnvironmentVariable("SUPPLIERS");
            if (supplierSettingsFromEnvironment != null)
            {
                enabledSuppliers = supplierSettingsFromEnvironment.Split(';').Select(i => i.Trim());
            }
            else
            {
                var updaterOptions = vaultClient.Get(Configuration["Suppliers:Options"]).Result;
                enabledSuppliers = updaterOptions["enabled"].Split(';').Select(i => i.Trim());
            }
            

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
            services.AddSingleton<TokenProvider>();
            services.AddTransient<JsonSerializer>();

            services.Configure<TokenRequestSettings>(options =>
            {
                var uri = new Uri(new Uri(authorityUrl), "/connect/token");
                options.TokenRequestUrl = uri.ToString();
                options.ClientId = Configuration["Identity:ClientId"];
                options.ClientSecret = clientSecret;
                options.Scopes = new[] {"edo", "connectors"};

                var disableCachingSetting = Environment.GetEnvironmentVariable("DISABLE_TOKEN_CACHE");

                options.IsCachingDisabled = bool.TryParse(disableCachingSetting, out var disableCache) && disableCache;
            });

            services.AddHttpClient(HttpClientNames.Identity, client =>
            {
                client.BaseAddress = new Uri(authorityUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            }).AddPolicyHandler(HttpClientPolicies.GetStandardRetryPolicy());

            services.AddHttpClient(HttpClientNames.EdoApi, client =>
                {
                    client.BaseAddress = new Uri(edoApiUrl);
                    client.Timeout = TimeSpan.FromMinutes(5);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(HttpClientPolicies.GetStandardRetryPolicy())
                .AddHttpMessageHandler<ProtectedApiBearerTokenHandler>();

            AddConnectorsHttpClients(services, supplierPaths);

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

                o.Suppliers = enabledSuppliers;
                o.UpdateMode = Enum.TryParse<UpdateMode>(Environment.GetEnvironmentVariable("UPDATE_MODE"), out var updateMode)
                    ? updateMode
                    : UpdateMode.Differential;
            });

            services.AddHostedService<LocationUpdaterHostedService>();
            services.AddNameNormalizationServices();
            services.AddHttpClient();
            services.AddHealthChecks();
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, LocationUpdaterContext context)
        {
            context.Database.Migrate();
            app.UseHealthChecks("/health");
        }


        private static void AddConnectorsHttpClients(IServiceCollection services,
            Dictionary<string, string> supplierPaths)
        {
            foreach (var (supplierKey, basePath) in supplierPaths)
            {
                services.AddHttpClient(supplierKey, client =>
                    {
                        client.BaseAddress = client.BaseAddress = new Uri(basePath);
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.Timeout = TimeSpan.FromMinutes(10);
                    }).SetHandlerLifetime(TimeSpan.FromMinutes(10))
                    .AddPolicyHandler(HttpClientPolicies.GetStandardRetryPolicy())
                    .AddHttpMessageHandler<ProtectedApiBearerTokenHandler>();
            }
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