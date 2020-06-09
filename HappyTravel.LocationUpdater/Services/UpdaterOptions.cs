using System;
using System.Collections.Generic;

namespace HappyTravel.LocationUpdater.Services
{
    public class UpdaterOptions
    {
        public IEnumerable<string> Connectors { get; set; }
        public int BatchSize { get; set; }
        public TimeSpan UploadRequestDelay { get; set; }
    }
}