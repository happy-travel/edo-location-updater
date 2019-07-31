using System;
using HappyTravel.LocationUpdater.Models.Enums;
using Newtonsoft.Json;

namespace HappyTravel.LocationUpdater.Models
{
    public readonly struct Location
    {
        [JsonConstructor]
        public Location(string name, string locality, string country, GeoPoint coordinates, int distance, PredictionSources source, LocationTypes type)
        {
            Name = name;
            Locality = locality;
            Country = country;
            Coordinates = coordinates;
            Distance = distance;
            Source = source;
            Type = type;
        }


        public Location(Location location, int distance, PredictionSources source)
            : this(location.Name, location.Locality, location.Country, location.Coordinates, distance, source, location.Type)
        { }


        public GeoPoint Coordinates { get; }
        public string Country { get; }
        public int Distance { get; }
        public string Locality { get; }
        public string Name { get; }
        public PredictionSources Source { get; }
        public LocationTypes Type { get; }
    }
}
