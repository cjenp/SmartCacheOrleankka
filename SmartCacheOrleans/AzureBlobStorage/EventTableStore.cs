using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Serilog;

namespace AzureBlobStorage
{
    public interface IEventTableStore
    {
        Task<EventTableStoreStream> ProvisonEventStream(string IdActor);
    }

    public class EventTableStore : IEventTableStore
    {
        private JsonSerializerSettings jsonSerializerSettings;
        private CloudStorageAccount cloudStorageAccount;
        private CloudTable cloudTable;
        private ILogger log;

        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            Culture = CultureInfo.InvariantCulture,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            TypeNameHandling = TypeNameHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.None
        };

        public EventTableStore(IOptions<EventTableStoreSettings> settings, JsonSerializerSettings JsonSerializerSettings = null)
        {
            cloudStorageAccount = CloudStorageAccount.Parse(settings.Value.AzureConnectionString);
            cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference(settings.Value.TableName);
            jsonSerializerSettings = JsonSerializerSettings;
            log = null;

            if (JsonSerializerSettings == null)
                jsonSerializerSettings = JsonSerializerSettings;
        }

        public EventTableStore(IOptions<EventTableStoreSettings> settings, ILogger Log, JsonSerializerSettings JsonSerializerSettings = null)
        {
            cloudStorageAccount = CloudStorageAccount.Parse(settings.Value.AzureConnectionString);
            cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference(settings.Value.TableName);
            jsonSerializerSettings = JsonSerializerSettings;
            log = Log;

            if (JsonSerializerSettings == null)
                jsonSerializerSettings = JsonSerializerSettings;
        }

        
        public async Task<EventTableStoreStream> ProvisonEventStream(string actorId)
        {
            await cloudTable.CreateIfNotExistsAsync();
            return new EventTableStoreStream(cloudTable, actorId, jsonSerializerSettings,log);
        }
    }
}
