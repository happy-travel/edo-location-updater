using System;

namespace HappyTravel.LocationUpdater.Services
{
    public class UpdaterOptions
    {
        public int BatchSize { get; set; }
        public TimeSpan UploadRequestDelay { get; set; }
    }
}