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
using Microsoft.Extensions.DependencyInjection;
using CacheGrainInter;
using AzureBlobStorage;
using Newtonsoft.Json;
using System.Globalization;

namespace SiloHost
{
    public class Program
    {
        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            Culture = CultureInfo.GetCultureInfo("en-US"),
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            TypeNameHandling = TypeNameHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.None
        };

        private static CloudStorageAccount csAccount;
        public static int Main(string[] args)
        {
            csAccount = CloudStorageAccount.DevelopmentStorageAccount;
            SS.Table = SetupTable(csAccount).GetAwaiter().GetResult();
            return RunMainAsync().Result;
        }

        static async Task<CloudTable> SetupTable(CloudStorageAccount account)
        {
            var table = account
                .CreateCloudTableClient()
                .GetTableReference("smartCache");

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
                .ConfigureServices(d=>d.AddSingleton<ISnapshotStore>(new SnapshotStore(csAccount, SerializerSettings)))
                .UseInMemoryReminderService()
                .UseOrleankka();

            var host = builder.Build();
            
            await host.StartAsync();
            return host;
        }
    }

}
