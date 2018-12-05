using System.Globalization;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

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

        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
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

        // predpostavimo da container obstaja
        public EventTableStore(string AzureCloudCredentials, string TableName, JsonSerializerSettings JsonSerializerSettings=SerializerSettings)
        {
            cloudStorageAccount = new CloudStorageAccount(AzureCloudCredentials, true);
            cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference(TableName);
            jsonSerializerSettings = JsonSerializerSettings;
        }

        public async Task<EventTableStoreStream> ProvisonEventStream(string actorId)
        {
            await cloudTable.CreateIfNotExistsAsync();
            return new EventTableStoreStream(cloudTable, actorId, jsonSerializerSettings);
        }
    }
}
