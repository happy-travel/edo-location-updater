using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.LocationUpdater.Infrastructure;
using HappyTravel.LocationUpdater.Models;
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
            IOptions<UpdaterOptions> options)
        {
            _clientFactory = clientFactory;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
            _options = options.Value;

            _serializer = new JsonSerializer();
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
            using var client = _clientFactory.CreateClient(HttpClientNames.NetstormingConnector);
            using var response = await client.GetAsync(GetLocationsRequestPath);

            if (!response.IsSuccessStatusCode)
            {
                var error =
                    $"Failed to get locations from {client.BaseAddress}{GetLocationsRequestPath} with status code {response.StatusCode}, message: '{response.ReasonPhrase}";
                _logger.LogError(LoggerEvents.GetLocationsRequestFailure, error);
                throw new HttpRequestException(error);
            }

            _logger.LogInformation(LoggerEvents.GetLocationsRequestSuccess,
                $"Locations from {client.BaseAddress}{GetLocationsRequestPath} loaded successfully");

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(streamReader);

            return _serializer.Deserialize<List<Location>>(jsonTextReader);
        }

        private async Task UploadLocations(List<Location> locations)
        {
            using var client = _clientFactory.CreateClient(HttpClientNames.EdoApi);

            foreach (var batch in ListHelper.SplitList(locations, _options.BatchSize))
            {
                await Task.Delay(_options.UploadRequestDelay);

                var json = JsonConvert.SerializeObject(batch);
                using var response = await client.PostAsync(UploadLocationsRequestPath,
                    new StringContent(json, Encoding.UTF8, "application/json"));

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
        
        private const string GetLocationsRequestPath = "/locations";
        private const string UploadLocationsRequestPath = "/en/api/1.0/locations";
        private readonly IHostApplicationLifetime _applicationLifetime;

        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<LocationUpdaterHostedService> _logger;
        private readonly UpdaterOptions _options;
        private readonly JsonSerializer _serializer;
    }
}