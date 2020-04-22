using HappyTravel.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.LocationUpdater.Infrastructure.Extensions
{
    public static class ServiceScope
    {
        public static LocationUpdaterContext GetLocationUpdaterContext(this IServiceScope scope)
        {
            return scope.ServiceProvider.GetRequiredService<LocationUpdaterContext>();
        }
    }
}