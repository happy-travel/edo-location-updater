using System.IO;
using System.Reflection;
using HappyTravel.EdoLocationUpdater.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HappyTravel.EdoLocationUpdater.Data
{
    public class LocationUpdaterContextFactory : IDesignTimeDbContextFactory<LocationUpdaterContext>
    {
        public LocationUpdaterContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("appsettings.json",
                    false,
                    true)
                .AddEnvironmentVariables()
                .Build();

            using var vaultClient = StartupHelper.CreateVaultClient(configuration);
            vaultClient.Login(configuration[configuration["Vault:Token"]]).GetAwaiter().GetResult();

            var connectionString = StartupHelper.GetDbConnectionString(vaultClient, configuration);

            var dbContextOptions = new DbContextOptionsBuilder<LocationUpdaterContext>();
            dbContextOptions.UseNpgsql(connectionString,
                builder => builder.UseNetTopologySuite());

            return new LocationUpdaterContext(dbContextOptions.Options);
        }
    }
}