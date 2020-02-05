using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.LocationUpdater.Infrastructure;
using HappyTravel.LocationUpdater.Models;
using HappyTravel.LocationUpdater.Models.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.LocationUpdater.Services
{
    public class LocationUpdaterHostedService : IHostedService
    {
        public LocationUpdaterHostedService(IHttpClientFactory clientFactory,
            IHostApplicationLifetime applicationLifetime,
            ILogger<LocationUpdaterHostedService> logger,
            IOptions<UpdaterOptions> options,
            JsonSerializer serializer)
        {
            _clientFactory = clientFactory;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
            _options = options.Value;

            _serializer = serializer;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            return LoadLocations();
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }


        private async Task LoadLocations()
        {
            _logger.LogInformation(LoggerEvents.ServiceStarting, "Started loading locations...");

            try
            {
                var locations = await FetchLocations();
                var processedLocations = LocationProcessor.ProcessLocations(locations);
                await UploadLocations(processedLocations);

                _logger.LogInformation(LoggerEvents.ServiceStopping, "Finished loading locations...");
            }
            catch (Exception ex)
            {
                _logger.LogError(LoggerEvents.ServiceError, ex.Message);
            }
            finally
            {
                _applicationLifetime.StopApplication();
            }
        }

        private async Task<List<Location>> FetchLocations()
        {
            var lastModified = await GetLastModifiedDate();
            var netstormingLocations = await FetchLocations(HttpClientNames.NetstormingConnector, lastModified);
            var illusionsLocations = await FetchLocations(HttpClientNames.Illusions, lastModified);

            var intersectedLocations = netstormingLocations.Intersect(illusionsLocations).ToList();

            return netstormingLocations
                .Except(intersectedLocations)
                .Select(l => new Location(l, new List<DataProviders> {DataProviders.Netstorming}))
                .Union(intersectedLocations.Select(l
                    => new Location(l, new List<DataProviders> {DataProviders.Netstorming, DataProviders.Illusions})))
                .Union(illusionsLocations.Except(intersectedLocations).Select(l
                    => new Location(l, new List<DataProviders> {DataProviders.Illusions}))).ToList();
        }

        
        private async Task<DateTime> GetLastModifiedDate()
        {
            using var client = _clientFactory.CreateClient(HttpClientNames.EdoApi);
            using var response = await client.GetAsync(GetLocationsModifiedDateRequestPath);

            if (!response.IsSuccessStatusCode)
            {
                var error =
                    $"Failed to get locations last modified date with status code '{response.StatusCode}', message '{response.ReasonPhrase}'";
                _logger.LogError(LoggerEvents.GetLocationsModifiedRequestFailure, error);
                throw new HttpRequestException(error);
            }

            var lastModified = JsonConvert.DeserializeObject<DateTime>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation(LoggerEvents.GetLocationsModifiedRequestSuccess, $"Last locations modified was fetched successfully: '{lastModified}'");
            return lastModified;
        }


        private async Task<List<Location>> FetchLocations(string providerName, DateTime lastModified)
        {
            using (var client = _clientFactory.CreateClient(providerName))
                //TODO: get last modified date from db
            using (var response = await client.GetAsync(GetLocationsRequestPath
                + lastModified.ToString("s")))
            {
                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        $"Failed to get locations from {client.BaseAddress}{GetLocationsRequestPath} with status code {response.StatusCode}, message: '{response.ReasonPhrase}";
                    _logger.LogError(LoggerEvents.GetLocationsRequestFailure, error);
                    throw new HttpRequestException(error);
                }

                _logger.LogInformation(LoggerEvents.GetLocationsRequestSuccess,
                    $"Locations from {client.BaseAddress}{GetLocationsRequestPath} loaded successfully");

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var streamReader = new StreamReader(stream))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                    return _serializer.Deserialize<List<Location>>(jsonTextReader);
            }
        }

        private async Task UploadLocations(List<Location> locations)
        {
            using (var client = _clientFactory.CreateClient(HttpClientNames.EdoApi))
            {
                foreach (var batch in ListHelper.SplitList(locations, _options.BatchSize))
                {
                    await Task.Delay(_options.UploadRequestDelay);
                    await UploadBatch(batch, client);
                }
            }
        }

        private async Task UploadBatch(List<Location> batch, HttpClient client)
        {
            try
            {
                var json = JsonConvert.SerializeObject(batch);
                using (var response = await client.PostAsync(UploadLocationsRequestPath,
                    new StringContent(json, Encoding.UTF8, "application/json")))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var error =
                            $"Failed to upload {batch.Count} locations from {client.BaseAddress}{UploadLocationsRequestPath} with status code {response.StatusCode}, message: '{response.ReasonPhrase}";
                        _logger.LogError(LoggerEvents.UploadLocationsRequestFailure, error);
                        throw new HttpRequestException(error);
                    }

                    _logger.LogInformation(LoggerEvents.UploadLocationsRequestSuccess,
                        $"Uploading {batch.Count} locations to {client.BaseAddress}{UploadLocationsRequestPath} completed successfully");
                }
            }
            catch (HttpRequestException)
            {
                // If the batch cannot be divided we cannot do anything
                if (batch.Count < 2)
                {
                    var problemLocationNames = string.Join(";", batch.Select(b => b.Name));
                    _logger.LogError(LoggerEvents.UploadLocationsRetryFailure,
                        $"Could not load locations: '{problemLocationNames}'");
                    throw;
                }


                // We'll try to do this with a smaller portion of locations.
                var smallerBatches = ListHelper.SplitList(batch, batch.Count / 2);
                foreach (var smallerBatch in smallerBatches)
                {
                    _logger.LogInformation(LoggerEvents.UploadLocationsRetry,
                        $"Retrying upload locations with smaller batch size {smallerBatch.Count}");

                    await UploadBatch(smallerBatch, client);
                }
            }
        }

        private const string GetLocationsRequestPath = "locations/";
        private const string GetLocationsModifiedDateRequestPath = "/en/api/1.0/locations/lastModified";
        private const string UploadLocationsRequestPath = "/en/api/1.0/locations";
        private readonly IHostApplicationLifetime _applicationLifetime;


        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<LocationUpdaterHostedService> _logger;
        private readonly UpdaterOptions _options;
        private readonly JsonSerializer _serializer;
    }
}