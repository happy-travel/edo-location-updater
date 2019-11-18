﻿using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.LocationUpdater.Models.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PredictionSources
    {
        [EnumMember(Value = "geocoder")]
        Google,

        [EnumMember(Value = "netstorming-connector")]
        NetstormingConnector
    }
}