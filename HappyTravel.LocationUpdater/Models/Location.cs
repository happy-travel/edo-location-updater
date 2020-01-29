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
            DefaultFromLocalizedCountry = LocalizationHelper.GetDefaultFromLocalizedName(country).ToUpper();
            DefaultFromLocalizedLocality = LocalizationHelper.GetDefaultFromLocalizedName(locality).ToUpper();
            DefaultFromLocalizedName = LocalizationHelper.GetDefaultFromLocalizedName(name).ToUpper();
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

        public string DefaultFromLocalizedCountry { get; }
        public string DefaultFromLocalizedLocality { get; }
        public string DefaultFromLocalizedName { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is Location))
                return false;

            var otherLocation = (Location) obj;

            return (DefaultFromLocalizedName, DefaultFromLocalizedLocality,
                    DefaultFromLocalizedCountry, Coordinates, Distance, Source, Type) ==
                (otherLocation.DefaultFromLocalizedName, otherLocation.DefaultFromLocalizedLocality,
                    otherLocation.DefaultFromLocalizedCountry, otherLocation.Coordinates, otherLocation.Distance,
                    otherLocation.Source, otherLocation.Type);
        }

        public override int GetHashCode()
            => (ParsedName: DefaultFromLocalizedName, ParsedLocality: DefaultFromLocalizedLocality,
                ParsedCountry: DefaultFromLocalizedCountry, Coordinates, Distance, Source, Type).GetHashCode();
    }
}