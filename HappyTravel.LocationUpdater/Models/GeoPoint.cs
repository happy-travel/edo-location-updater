using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HappyTravel.LocationUpdater.Models
{
    public readonly struct GeoPoint
    {
        [JsonConstructor]
        public GeoPoint([Range(-180, 180)] double longitude, [Range(-90, 90)] double latitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }


        public double Latitude { get; }

        public double Longitude { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is GeoPoint))
                return false;

            var otherGeoPoint = (GeoPoint) obj;
            return Latitude.CompareTo(otherGeoPoint.Latitude) == 0
                && Longitude.CompareTo(otherGeoPoint.Longitude) == 0;
        }

        public override int GetHashCode() => (Longitude, Latitude).GetHashCode();

        public static bool operator ==(GeoPoint left, GeoPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeoPoint left, GeoPoint right)
        {
            return !left.Equals(right);
        }
    }
}