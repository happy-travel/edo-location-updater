using System;
using System.Collections.Generic;

namespace HappyTravel.LocationUpdater.Services
{
    public class UpdaterOptions
    {
        public List<string> DataProviders { get; set; }
        public int BatchSize { get; set; }
        public TimeSpan UploadRequestDelay { get; set; }
    }
}