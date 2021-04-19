using System;
using System.Collections.Generic;
using HappyTravel.EdoLocationUpdater.Data.Models;
using HappyTravel.EdoContracts.GeoData.Enums;

namespace HappyTravel.EdoLocationUpdater.Updater.Services
{
    internal static class LocationProcessor
    {
        private const int DefaultSearchDistanceForDestinations = 3_000;
        private const int DefaultSearchDistanceForHotels = 100;
        private const int DefaultSearchDistanceForLandmarks = 1_000;
        private const int DefaultSearchDistanceForCity = 20_000;
        private const int DefaultSearchDistanceForCityZone = 2_000;
        private const int DefaultSearchDistanceForCountry = 200_000;

        public static List<Location> ProcessLocations(List<Location> locations)
        {
            var processedLocations = new List<Location>(locations.Count);
            foreach (var location in locations)
                processedLocations.Add(ProcessLocation(location));

            return processedLocations;
        }

        private static Location ProcessLocation(in Location location)
        {
            switch (location.Type)
            {
                case LocationTypes.Destination:
                    return new Location(location, DefaultSearchDistanceForDestinations,
                        PredictionSources.Interior);
                case LocationTypes.Accommodation:
                    return new Location(location, DefaultSearchDistanceForHotels,
                        PredictionSources.Interior);
                case LocationTypes.Landmark:
                    return new Location(location, DefaultSearchDistanceForLandmarks,
                        PredictionSources.Interior);
                case LocationTypes.Location:
                {
                    var distance = DefaultSearchDistanceForCountry;
                    if (!string.IsNullOrWhiteSpace(location.Locality))
                    {
                        distance = DefaultSearchDistanceForCity;
                        if (!string.IsNullOrWhiteSpace(location.Name))
                            distance = DefaultSearchDistanceForCityZone;
                    }

                    return new Location(location, distance, PredictionSources.Interior);
                }
                case LocationTypes.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(location));
            }
        }
    }
}