using System;
using System.Net;
using System.Threading.Tasks;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using CacheGrainImpl;
using Microsoft.WindowsAzure.Storage.Table;
using Orleankka.Cluster;
using Orleans.Storage;

namespace SiloHost
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var account = CloudStorageAccount.DevelopmentStorageAccount;
            SS.Table = SetupTable(account).GetAwaiter().GetResult();
            return RunMainAsync().Result;
        }

        static async Task<CloudTable> SetupTable(CloudStorageAccount account)
        {
            var table = account
                .CreateCloudTableClient()
                .GetTableReference("ssexample");

            await table.CreateIfNotExistsAsync();
            return table;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSilo();

                Console.WriteLine("Silo started, press Enter to stop.");
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

        private static async Task<ISiloHost> StartSilo()
        {
            // silo config
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "nejcSC";
                    options.ServiceId = "SmartCache";
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(CacheGrainImpl.Domain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole())
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                .AddSimpleMessageStreamProvider("sms")
                .ConfigureApplicationParts(x => x.AddApplicationPart(typeof(MemoryGrainStorage).Assembly))
                .UseInMemoryReminderService()
                .UseOrleankka();


            var host = builder.Build();
            
            await host.StartAsync();
            return host;
        }
    }
}
