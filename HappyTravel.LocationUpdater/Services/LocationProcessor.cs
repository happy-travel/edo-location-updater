using System;
using System.Collections.Generic;
using HappyTravel.LocationUpdater.Models;
using HappyTravel.LocationUpdater.Models.Enums;

namespace HappyTravel.LocationUpdater.Services
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
            for (var i = 0; i < locations.Count - 1; i++)
                processedLocations.Add(ProcessLocation(locations[i]));

            return processedLocations;
        }

        private static Location ProcessLocation(in Location location)
        {
            switch (location.Type)
            {
                case LocationTypes.Destination:
                    return new Location(location, DefaultSearchDistanceForDestinations,
                        PredictionSources.NetstormingConnector);
                case LocationTypes.Accommodation:
                    return new Location(location, DefaultSearchDistanceForHotels,
                        PredictionSources.NetstormingConnector);
                case LocationTypes.Landmark:
                    return new Location(location, DefaultSearchDistanceForLandmarks,
                        PredictionSources.NetstormingConnector);
                case LocationTypes.Location:
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
                case LocationTypes.Unknown:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}