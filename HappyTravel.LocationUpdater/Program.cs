using System.Threading.Tasks;
using HappyTravel.StdOutLogger.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace HappyTravel.LocationUpdater
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateWebHostBuilder(args)
                .Build()
                .RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var environment = hostingContext.HostingEnvironment;

                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true,
                            reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .UseSentry((hostBuilder, options) =>
                {
                    options.Dsn = hostBuilder.Configuration["Logging:Sentry:Endpoint"];
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders()
                        .AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddStdOutLogger(setup =>
                    {
                        setup.IncludeScopes = false;
                        setup.UseUtcTimestamp = true;
                    });
                });
    }
}
