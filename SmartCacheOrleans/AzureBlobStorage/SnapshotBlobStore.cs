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
    public class SnapshotBlobStore : ISnapshotStore
    {
        private JsonSerializerSettings jsonSerializerSettings;
        private CloudStorageAccount cloudStorageAccount;
        private CloudTable cloudTable;
        private CloudBlobClient blobClient;
        private CloudBlobContainer blobContainer;
        private string containerName;
        private ILogger log;

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

        public SnapshotBlobStore(string AzureConnectionString, string ContainerName, string TableName)
        {
            cloudStorageAccount = CloudStorageAccount.Parse(AzureConnectionString);
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference(TableName);
            jsonSerializerSettings = SerializerSettings;
            containerName = ContainerName;
        }

        public SnapshotBlobStore(IOptions<SnapshotBlobStoreSettings> settings, JsonSerializerSettings JsonSerializerSettings = null)
        {
            cloudStorageAccount = CloudStorageAccount.Parse(settings.Value.AzureConnectionString);
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference(settings.Value.TableName);
            jsonSerializerSettings = JsonSerializerSettings;
            containerName = settings.Value.ContainerName;

            if (JsonSerializerSettings == null)
                jsonSerializerSettings = JsonSerializerSettings;
        }
        
        public SnapshotBlobStore(IOptions<SnapshotBlobStoreSettings> settings,ILogger Log, JsonSerializerSettings JsonSerializerSettings = null)
        {
            cloudStorageAccount = CloudStorageAccount.Parse(settings.Value.AzureConnectionString);
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference(settings.Value.TableName);
            jsonSerializerSettings = JsonSerializerSettings;
            containerName = settings.Value.ContainerName;
            log = Log;

            if(JsonSerializerSettings == null)
                jsonSerializerSettings = JsonSerializerSettings;
        }

        public async Task<SnapshotBlobStream> ProvisonSnapshotStream(string actorId)
        {
            await cloudTable.CreateIfNotExistsAsync();
            blobContainer = blobClient.GetContainerReference(containerName);
            await blobContainer.CreateIfNotExistsAsync();
            await blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Container });

            return new SnapshotBlobStream(actorId, blobContainer, SerializerSettings, new EventTableStoreStream(cloudTable, actorId, jsonSerializerSettings,log),log);
        }
    }
}
