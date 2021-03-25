using HappyTravel.EdoLocationUpdater.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.EdoLocationUpdater.Updater.Infrastructure.Extensions
{
    public static class ServiceScope
    {
        public static LocationUpdaterContext GetLocationUpdaterContext(this IServiceScope scope)
        {
            return scope.ServiceProvider.GetRequiredService<LocationUpdaterContext>();
        }
    }
}