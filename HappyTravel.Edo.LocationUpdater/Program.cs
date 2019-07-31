using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore;

namespace HappyTravel.Edo.LocationUpdater
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await CreateWebHostBuilder(args)
                .Build()
                .RunAsync();
        }


        private static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((context, config) =>
                {
                    var configuration = context.Configuration;
                    var level = (LogLevel) Enum.Parse(typeof(LogLevel), configuration["Logging:LogLevel:Default"]);

                    config.AddConsole();
                    config.SetMinimumLevel(level);
                })
                .UseStartup<Startup>()
                .UseKestrel();
    }
}
