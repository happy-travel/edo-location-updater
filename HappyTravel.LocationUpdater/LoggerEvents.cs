using Microsoft.Extensions.Logging;

namespace HappyTravel.LocationUpdater
{
    internal static class LoggerEvents
    {
        public static EventId ServiceStarting = new EventId(20000);
        public static EventId ServiceStopping = new EventId(20001);
        public static EventId ServiceError = new EventId(20002);
        public static EventId GetLocationsRequestFailure = new EventId(20010);
        public static EventId GetLocationsRequestSuccess = new EventId(20011);
        public static EventId UploadLocationsRequestFailure = new EventId(20020);
        public static EventId UploadLocationsRequestSuccess = new EventId(20021);
    }
}