﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.LocationUpdater.Models;
using HappyTravel.LocationUpdater.Models.Enums;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace HappyTravel.LocationUpdater.Services
{
    public class LocationUpdaterHostedService : IHostedService
    {
        public LocationUpdaterHostedService(IHttpClientFactory clientFactory, IHostApplicationLifetime applicationLifetime)
        {
            _clientFactory = clientFactory;
            _applicationLifetime = applicationLifetime;

            _serializer = new JsonSerializer();
        }


        public Task StartAsync(CancellationToken cancellationToken) => GetLocations();


        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;


        private async Task GetLocations()
        {
            List<Location> locations;

            using (var client = _clientFactory.CreateClient(HttpClientNames.NetstormingConnector))
            using (var response = await client.GetAsync($"/api/1.0/locations/{DateTime.MinValue:O}"))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
                locations = _serializer.Deserialize<List<Location>>(jsonTextReader);

            await ProcessLocations(locations);
            
            _applicationLifetime.StopApplication();
        }


        private static Location ProcessLocation(in Location location)
        {
            var distance = DefaultSearchDistanceForCountry;
            if (!string.IsNullOrWhiteSpace(location.Locality))
            {
                distance = DefaultSearchDistanceForCity;
                if (!string.IsNullOrWhiteSpace(location.Name))
                    distance = DefaultSearchDistanceForCityZone;
            }

            return new Location(location, distance, PredictionSources.NetstormingConnector);
        }


        private Task ProcessLocations(List<Location> locations)
        {
            var processedLocations = new List<Location>();
            foreach (var location in locations)
                switch (location.Type)
                {
                    case LocationTypes.Destination:
                        processedLocations.Add(new Location(location, DefaultSearchDistanceForDestinations, PredictionSources.NetstormingConnector));
                        break;
                    case LocationTypes.Accommodation:
                        processedLocations.Add(new Location(location, DefaultSearchDistanceForHotels, PredictionSources.NetstormingConnector));
                        break;
                    case LocationTypes.Landmark:
                        processedLocations.Add(new Location(location, DefaultSearchDistanceForLandmarks, PredictionSources.NetstormingConnector));
                        break;
                    case LocationTypes.Location:
                        processedLocations.Add(ProcessLocation(location));
                        break;
                    case LocationTypes.Unknown:
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            return UploadLocations(processedLocations);
        }


        private async Task UploadLocations(List<Location> locations)
        {
            var json = JsonConvert.SerializeObject(locations);
            using (var client = _clientFactory.CreateClient(HttpClientNames.EdoApi))
            using (var _ = await client.PostAsync("/api/1.0/locations/" + PredictionSources.NetstormingConnector,
                new StringContent(json, Encoding.UTF8, "application/json")))
            { }
        }


        private const int DefaultSearchDistanceForDestinations = 3_000;
        private const int DefaultSearchDistanceForHotels = 100;
        private const int DefaultSearchDistanceForLandmarks = 1_000;
        private const int DefaultSearchDistanceForCity = 20_000;
        private const int DefaultSearchDistanceForCityZone = 2_000;
        private const int DefaultSearchDistanceForCountry = 200_000;
        
        private readonly IHttpClientFactory _clientFactory;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly JsonSerializer _serializer;
    }
}
