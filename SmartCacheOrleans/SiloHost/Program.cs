using System;
using System.Net;
using System.Threading.Tasks;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleankka.Cluster;
using Microsoft.Extensions.DependencyInjection;
using AzureBlobStorage;
using Newtonsoft.Json;
using System.Globalization;
using Serilog;
using ILogger = Serilog.ILogger;
using Microsoft.Extensions.Configuration;
using System.IO;
using CacheGrainImpl;

namespace SiloHost
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var log = new LoggerConfiguration()
                            .WriteTo.Seq("http://localhost:5341")
                            .WriteTo.Console()
                            .Enrich.FromLogContext()
                            .CreateLogger();

                var host = await StartSilo(log);

                log.Information("Silo started");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 0;
            }
        }

        private static async Task<ISiloHost> StartSilo(ILogger log)
        {
            var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var Configuration = configurationBuilder.Build();

            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "nejcSC";
                    options.ServiceId = "SmartCache";
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(CacheGrainImpl.Domain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddSerilog(log).Services.AddSingleton(log))
                .Configure<StreamProjectionSettings>(Configuration.GetSection(nameof(StreamProjectionSettings)))
                .Configure<SnapshotBlobStoreSettings>(Configuration.GetSection(nameof(SnapshotBlobStoreSettings)))
                .Configure<EventTableStoreSettings>(
                    options =>
                    {
                        options.AzureConnectionString = "UseDevelopmentStorage=true";
                        options.TableName = "tableName";
                    }
                )
                .ConfigureServices(d => d.AddSingleton<SnapshotBlobStore>())
                .ConfigureServices(d => d.AddSingleton<ISnapshotStore>(s=>s.GetService<SnapshotBlobStore>()))
                .ConfigureServices(d => d.AddSingleton<EventTableStore>())
                .ConfigureServices(d => d.AddSingleton<IEventTableStore>(s => s.GetService<EventTableStore>()))
                .UseInMemoryReminderService()
                .AddSimpleMessageStreamProvider("SMSProvider")
                .AddMemoryGrainStorage("PubSubStore")
                .UseOrleankka();

            var host = builder.Build();
            
            await host.StartAsync();
            return host;
        }
    }

}
