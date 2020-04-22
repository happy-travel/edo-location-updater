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
        public static EventId GetLocationsModifiedRequestFailure = new EventId(20012);
        public static EventId GetLocationsModifiedRequestSuccess = new EventId(20013);
        public static EventId UploadLocationsRequestFailure = new EventId(20020);
        public static EventId UploadLocationsRequestSuccess = new EventId(20021);
        public static EventId UploadLocationsRetry = new EventId(20022);
        public static EventId UploadLocationsRetryFailure = new EventId(20023);
        public static EventId DeserializeConnectorResponseFailure = new EventId(20024);
        public static EventId StartLocationsDownloadingToDb = new EventId(20025);
        public static EventId StartLocationsUploadingToEdo = new EventId(20026);
        public static EventId GetLocationsLastModifiedData = new EventId(20027);
        public static EventId DownloadLocationsFromConnectorToDb = new EventId(20028);
        public static EventId UploadLocationsToEdo = new EventId(20029);
        public static EventId RemovePreviousLocationsFromDb = new EventId(20030);
    }
}