using System;
using System.Collections.Generic;

namespace HappyTravel.EdoLocationUpdater.Updater.Services
{
    public class UpdaterOptions
    {
        public IEnumerable<string> Suppliers { get; set; }
        public int BatchSize { get; set; }
        public TimeSpan UploadRequestDelay { get; set; }
        public UpdateMode UpdateMode { get; set; }
    }
}