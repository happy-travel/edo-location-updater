﻿using System.Collections.Generic;
using HappyTravel.EdoLocationUpdater.Common.Models;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.Geography;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace HappyTravel.EdoLocationUpdater.Data.Models
{
    public class Location
    {
        public Point Coordinates { get; set; }
        public string Country { get; set; }
        public int Distance { get; set; }
        public string Locality { get; set; }
        public string Name { get; set; }
        public PredictionSources Source { get; set; }
        public LocationTypes Type { get; set; }
        public List<Suppliers> Suppliers { get; set; }

        
        public Location(){}
        
        
        [JsonConstructor]
        public Location(GeoPoint coordinates, string country, int distance, string locality, string name, PredictionSources source, LocationTypes type)
        {
            Coordinates = new Point(coordinates.Longitude, coordinates.Latitude);
            Country = country;
            Distance = distance;
            Locality = locality;
            Name = name;
            Source = source;
            Type = type;
        }
        

        public Location(Location location, int distance, PredictionSources predictionSources)
        {
            Id = location.Id;
            Coordinates = location.Coordinates;
            Country = location.Country;
            Locality = location.Locality;
            Name = location.Name;
            Type = location.Type;
            Suppliers = location.Suppliers;
            Distance = distance;
            Source = predictionSources;
        }
        
        
        public int Id { get; set; }
    }
}