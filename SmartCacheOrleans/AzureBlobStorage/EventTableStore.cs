using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Serilog;
using Streamstone;

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

            if (JsonSerializerSettings == null)
                jsonSerializerSettings = JsonSerializerSettings;
        }
        
        public async Task<EventTableStoreStream> ProvisonEventStream(string actorId)
        {
            await cloudTable.CreateIfNotExistsAsync();
            Partition partition=new Partition(cloudTable, actorId);
            Stream stream = new Stream(partition);
            return new EventTableStoreStream(partition, stream, actorId, jsonSerializerSettings);
        }
    }
}
