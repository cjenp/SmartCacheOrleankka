using System.Globalization;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

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

        // predpostavimo da container obstaja
        public SnapshotBlobStore()
        {
            cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference("table-x");
            containerName = "container-x";
            cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            jsonSerializerSettings = SerializerSettings;
        }

        // predpostavimo da container obstaja
        public SnapshotBlobStore(CloudStorageAccount CloudStorageAccount, JsonSerializerSettings JsonSerializerSettings, string ContainerName, string TableName)
        {
            cloudStorageAccount = CloudStorageAccount;
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference(TableName);
            jsonSerializerSettings = JsonSerializerSettings;
            containerName = ContainerName;
        }

        public async Task<SnapshotBlobStream> ProvisonSnapshotStream(string actorId)
        {
            await cloudTable.CreateIfNotExistsAsync();
            blobContainer = blobClient.GetContainerReference(containerName);
            await blobContainer.CreateIfNotExistsAsync();
            await blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Container });

            return new SnapshotBlobStream(actorId, blobContainer, SerializerSettings, new EventTableStoreStream(cloudTable, actorId, jsonSerializerSettings));
        }
    }
}
