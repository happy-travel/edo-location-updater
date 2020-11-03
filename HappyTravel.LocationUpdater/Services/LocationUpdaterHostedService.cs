using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Models;
using HappyTravel.Data.Models;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.LocationUpdater.Infrastructure.Extensions;
using HappyTravel.LocationUpdater.Infrastructure.JsonConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.LocationUpdater.Services
{
    public class LocationUpdaterHostedService : BackgroundService
    {
        public LocationUpdaterHostedService(IHttpClientFactory clientFactory,
            IHostApplicationLifetime applicationLifetime,
            ILogger<LocationUpdaterHostedService> logger,
            IOptions<UpdaterOptions> options,
            JsonSerializer serializer,
            IServiceScopeFactory serviceScopeFactory)
        {
            _clientFactory = clientFactory;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
            _options = options.Value;
            _serializer = serializer;
            _serviceScopeFactory = serviceScopeFactory;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(LoggerEvents.ServiceStarting, "Service started");
            try
            {
                _logger.LogInformation(LoggerEvents.RemovePreviousLocationsFromDb,
                    "Remove previous locations from the database");
                RemovePreviousLocations();

                _logger.LogInformation(LoggerEvents.StartLocationsDownloadingToDb,
                    "Start uploading locations to the database");
                await DownloadAndMergeLocations();

                _logger.LogInformation(LoggerEvents.StartLocationsUploadingToEdo, "Start downloading locations to Edo");
                await UploadLocationsToEdo();
            }
            catch (Exception ex)
            {
                _logger.LogError(LoggerEvents.ServiceError, ex.Message);
                throw;
            }

            _logger.LogInformation(LoggerEvents.ServiceStopping, "Service stopped");
            _applicationLifetime.StopApplication();
        }


        private void RemovePreviousLocations()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.GetLocationUpdaterContext();
            dbContext.ClearLocationsTable();
        }


        private async Task UploadLocationsToEdo()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.GetLocationUpdaterContext();

            using var client = _clientFactory.CreateClient(HttpClientNames.EdoApi);

            var skip = 0;
            var take = _options.BatchSize;

            List<Location> locations;
            do
            {
                locations = await dbContext.Locations
                    .OrderBy(l => l.Id)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                if (!locations.Any())
                    break;

                await UploadBatch(locations, client);

                _logger.LogInformation(LoggerEvents.UploadLocationsToEdo,
                    $"{skip + locations.Count} locations uploaded from the database to Edo");

                await Task.Delay(_options.UploadRequestDelay);

                skip += take;
            } while (locations.Count == take);
        }


        private async Task DownloadAndMergeLocations()
        {
            var lastModified = await GetLastModifiedDate();
            
            _logger.LogInformation(LoggerEvents.GetLocationsLastModifiedData,
                $"Last modified data from edo is '{lastModified:s}'");

            foreach (var (providerName, providerValue) in GetDataProviders(_options.DataProviders))
            {
                await DownloadAndAddLocationsToDb(providerName, providerValue, lastModified);
            }
        }


        private async Task DownloadAndAddLocationsToDb(string providerName, DataProviders providerType,
            DateTime lastModified)
        {
            using var httpClient = _clientFactory.CreateClient(providerName);

            foreach (var locationType in _uploadedLocationTypes)
            {
                var take = TakeFromConnectorLocationsCount;
                var skip = 0;
                List<Location> locations;
                do
                {
                    locations = await FetchLocations(httpClient, locationType, lastModified, skip, take);

                    var processedLocations = LocationProcessor.ProcessLocations(locations);

                    await UploadLocationsToDb(providerType, processedLocations);

                    _logger.LogInformation(LoggerEvents.DownloadLocationsFromConnectorToDb,
                        $"{skip + locations.Count} locations downloaded to the database from {providerName}: ");

                    skip += take;
                } while (locations.Count == take);
            }
        }


        private async Task UploadLocationsToDb(DataProviders providerType, List<Location> locations)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.GetLocationUpdaterContext();

            locations = locations.Select(l =>
            {
                l.Id = CalculateId(l);
                l.Name = SetBracketsIfEmpty(l.Name);
                l.Country = SetBracketsIfEmpty(l.Country);
                l.Locality = SetBracketsIfEmpty(l.Locality);
                l.DataProviders = new List<DataProviders> {providerType};
                return l;
            }).ToList();

            var uploadedLocations = await dbContext.Locations
                .Where(dbl => locations.Select(l => l.Id).Contains(dbl.Id))
                .ToListAsync();

            var newLocations = locations.Where(l => !uploadedLocations.Select(ul => ul.Id).Contains(l.Id))
                .GroupBy(p => p.Id)
                .Select(p => p.First())
                .ToList();

            UpdateUploadedLocations();

            dbContext.Locations.UpdateRange(uploadedLocations);

            dbContext.Locations.AddRange(newLocations);

            await dbContext.SaveChangesAsync();


            void UpdateUploadedLocations()
            {
                for (var i = 0; i < uploadedLocations.Count; i++)
                {
                    var uploadedLocation = uploadedLocations[i];
                    var updateLocation = locations.FirstOrDefault(l => l.Id == uploadedLocation.Id);

                    var wereLanguagesCombined = false;
                    if (updateLocation != null)
                        wereLanguagesCombined = CombineFieldsWithLanguageIfNeeded(uploadedLocation, updateLocation);

                    if (!uploadedLocation.DataProviders.Contains(providerType))
                        uploadedLocation.DataProviders.Add(providerType);
                    else if (!wereLanguagesCombined)
                        uploadedLocations.RemoveAt(i--);
                }
            }


            int CalculateId(Location location)
            {
                var defaultName = LanguageHelper.GetValue(location.Name, DefaultLanguageCode);
                var defaultLocality = LanguageHelper.GetValue(location.Locality, DefaultLanguageCode);
                var defaultCountry = LanguageHelper.GetValue(location.Country, DefaultLanguageCode);
                return DeterministicHash.Calculate(defaultName + defaultLocality + defaultCountry + location.Source +
                                                   location.Type + location.Coordinates);
            }


            string SetBracketsIfEmpty(string value) => string.IsNullOrEmpty(value) ? "{}" : value;
        }


        private bool CombineFieldsWithLanguageIfNeeded(Location original, Location update)
        {
            var wereCombined = false;

            if (original.Name != null && update.Name != null && !original.Name.Equals(update.Name))
            {
                original.Name = LanguageHelper.MergeLanguages(original.Name, update.Name);
                wereCombined = true;
            }

            if (original.Locality != null && update.Locality != null && !original.Locality.Equals(update.Locality))
            {
                original.Locality = LanguageHelper.MergeLanguages(original.Locality, update.Locality);
                wereCombined = true;
            }

            if (original.Country != null && update.Country != null && !original.Country.Equals(update.Country))
            {
                original.Country = LanguageHelper.MergeLanguages(original.Country, update.Country);
                wereCombined = true;
            }

            return wereCombined;
        }


        private async Task<List<Location>> FetchLocations(HttpClient httpClient, LocationTypes locationType,
            DateTime lastModified, int skip, int take)
        {
            var requestPath =
                $"locations/{lastModified:s}?{nameof(locationType)}={(int) locationType}&{nameof(skip)}={skip}&{nameof(take)}={take}";
            using var response = await httpClient.GetAsync(requestPath);
            if (!response.IsSuccessStatusCode)
            {
                var error =
                    $"Failed to get locations from {httpClient.BaseAddress}{requestPath} with status code {response.StatusCode}, message: '{response.ReasonPhrase}";
                _logger.LogError(LoggerEvents.GetLocationsRequestFailure, error);
                throw new HttpRequestException(error);
            }

            _logger.LogInformation(LoggerEvents.GetLocationsRequestSuccess,
                $"Locations from {httpClient.BaseAddress}{requestPath} loaded successfully");

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(stream);
            try
            {
                using var jsonTextReader = new JsonTextReader(streamReader);
                return _serializer.Deserialize<List<Location>>(jsonTextReader);
            }
            catch (Exception ex)
            {
                var error =
                    $"Failed to deserialize locations from {httpClient.BaseAddress}{requestPath}, error: {ex}";
                _logger.LogInformation(LoggerEvents.DeserializeConnectorResponseFailure, error);
                throw;
            }
        }


        private List<(string dataProvider, DataProviders enumValue)> GetDataProviders(IEnumerable<string> dataProviders)
        {
            var dataProvidersNameAndValue = new List<(string providerName, DataProviders enumValue)>();
            foreach (var dataProvider in dataProviders)
            {
                var dataProviderEnumName = new string(Char.ToUpper(dataProvider[0]) + dataProvider.Substring(1));

                if (!Enum.TryParse<DataProviders>(dataProviderEnumName, out var dataProviderEnumValue))
                    continue;
                dataProvidersNameAndValue.Add((dataProvider, dataProviderEnumValue));
            }

            return dataProvidersNameAndValue;
        }


        private async Task<DateTime> GetLastModifiedDate()
        {
            if (_options.UpdateMode == UpdateMode.Full)
                return DateTime.MinValue;
            
            using var edoClient = _clientFactory.CreateClient(HttpClientNames.EdoApi);
            using var response = await edoClient.GetAsync(GetLocationsModifiedDateRequestPath);

            if (!response.IsSuccessStatusCode)
            {
                var error =
                    $"Failed to get locations last modified date with status code '{response.StatusCode}', message '{response.ReasonPhrase}'";
                _logger.LogError(LoggerEvents.GetLocationsModifiedRequestFailure, error);
                throw new HttpRequestException(error);
            }

            var lastModified = JsonConvert.DeserializeObject<DateTime>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation(LoggerEvents.GetLocationsModifiedRequestSuccess,
                $"Last locations modified was fetched successfully: '{lastModified}'");
            return lastModified;
        }


        private async Task UploadBatch(List<Location> batch, HttpClient client, int attemptsToUpload = 10)
        {
            var json = JsonConvert.SerializeObject(batch, new PointConverter());
            var failedToUploadLocationsError =
                $"Failed to upload {batch.Count} locations to {client.BaseAddress}{UploadLocationsRequestPath}.";
            
            for (var i = 0; i < attemptsToUpload; i++)
            {
                try
                {
                    using var response = await client.PostAsync(UploadLocationsRequestPath,
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation(LoggerEvents.UploadLocationsRequestSuccess,
                            $"Uploading {batch.Count} locations to {client.BaseAddress}{UploadLocationsRequestPath} completed successfully");
                        return;
                    }
                    
                    _logger.LogError(LoggerEvents.UploadLocationsRequestFailure,
                        $"{failedToUploadLocationsError} Status code {response.StatusCode}, message: '{response.ReasonPhrase}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggerEvents.UploadLocationsRequestFailure,
                        $"{failedToUploadLocationsError} Exception occurred:{ex}");
                }
            }
            
            throw new Exception(failedToUploadLocationsError);
        }


        private const int TakeFromConnectorLocationsCount = 3000;
        private const string GetLocationsModifiedDateRequestPath = "/en/api/1.0/locations/last-modified-date";
        private const string UploadLocationsRequestPath = "/en/api/1.0/locations";
        private const string DefaultLanguageCode = "en";
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<LocationUpdaterHostedService> _logger;
        private readonly UpdaterOptions _options;
        private readonly JsonSerializer _serializer;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly List<LocationTypes> _uploadedLocationTypes = new List<LocationTypes>
        {
            LocationTypes.Location,
            LocationTypes.Accommodation
        };
    }
}