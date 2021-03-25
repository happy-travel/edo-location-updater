using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappyTravel.EdoLocationUpdater.Updater.Infrastructure.JsonConverters
{
    public class PointConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is Point point))
                return;
            
            var jObject = JObject.FromObject(new {longitude = point.X, latitude = point.Y }); 
            jObject.WriteTo(writer);
        }

        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        
        public override bool CanRead => false;

        
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Point);
        }
    }
}