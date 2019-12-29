using System;
using System.Threading.Tasks;
using HappyTravel.SentryLogger.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;

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
                        .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    var env = hostingContext.HostingEnvironment;
                    if (!env.IsDevelopment())
                    {
                        logging.AddEventSourceLogger()
                            .AddSentry(c =>
                            {
                                c.Endpoint = hostingContext.Configuration["Logging:Sentry:Endpoint"];
                            });
                    }
                })
                .UseSerilog((context, config) =>
                {
                    var configuration = context.Configuration;
                    var level = (LogEventLevel) Enum.Parse(typeof(LogEventLevel), configuration["Logging:LogLevel:Default"]);
                    config.MinimumLevel.Is(level)
                        .Enrich.FromLogContext();
                    
                    if (context.HostingEnvironment.IsProduction())
                    {
                        config.WriteTo.Console(new ElasticsearchJsonFormatter(renderMessageTemplate:false, inlineFields:true));
                    }
                    else
                    {
                        config.WriteTo.Console();
                    }
                });
    }
}
