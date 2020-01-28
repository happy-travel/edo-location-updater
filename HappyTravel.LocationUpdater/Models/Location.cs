using System;
using System.Collections.Generic;
using HappyTravel.LocationUpdater.Infrastructure;
using HappyTravel.LocationUpdater.Models.Enums;
using Newtonsoft.Json;

namespace HappyTravel.LocationUpdater.Models
{
    public readonly struct Location
    {
        [JsonConstructor]
        public Location(string name, string locality, string country, GeoPoint coordinates, int distance,
            PredictionSources source, LocationTypes type, List<DataProviders> dataProviders = null)
        {
            Name = name;
            Locality = locality;
            Country = country;
            Coordinates = coordinates;
            Distance = distance;
            Source = source;
            Type = type;
            DataProviders = dataProviders == null ? new List<DataProviders>() : dataProviders;
            //Name, Locality, Country we are getting in json format and for comparision we need only in default localization
            ParsedCountry = LocalizationHelper.GetDefaultFromLocalizedName(country).ToUpper();
            ParsedLocality = LocalizationHelper.GetDefaultFromLocalizedName(locality).ToUpper();
            ParsedName = LocalizationHelper.GetDefaultFromLocalizedName(name).ToUpper();
        }


        public Location(Location location, int distance, PredictionSources source)
            : this(location.Name, location.Locality, location.Country, location.Coordinates, distance, source,
                location.Type, location.DataProviders)
        {
        }

        public Location(Location location, List<DataProviders> dataProviders)
            : this(location.Name, location.Locality, location.Country, location.Coordinates, location.Distance,
                location.Source, location.Type, dataProviders)
        {
        }


        public GeoPoint Coordinates { get; }
        public string Country { get; }
        public int Distance { get; }
        public string Locality { get; }
        public string Name { get; }
        public PredictionSources Source { get; }
        public LocationTypes Type { get; }

        public List<DataProviders> DataProviders { get; }

        public readonly string ParsedCountry;
        public readonly string ParsedLocality;
        public readonly string ParsedName;

        public override bool Equals(object obj)
        {
            if (!(obj is Location))
                return false;

            var otherLocation = (Location) obj;

            return ParsedCountry == otherLocation.ParsedCountry
                && ParsedLocality == otherLocation.ParsedLocality
                && ParsedName == otherLocation.ParsedName
                && Coordinates.Latitude.CompareTo(otherLocation.Coordinates.Latitude) == 0 &&
                Coordinates.Longitude.CompareTo(otherLocation.Coordinates.Longitude) == 0
                && Source == otherLocation.Source
                && Type == otherLocation.Type;
        }

        public override int GetHashCode()
            => (ParsedName, ParsedLocality, ParsedCountry, Coordinates, Distance, Source, Type).GetHashCode();
    }
}