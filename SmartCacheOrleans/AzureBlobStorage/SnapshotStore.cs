using System.Globalization;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace AzureBlobStorage
{
    public class SnapshotStore : ISnapshotStore
    {
        private CloudBlobClient blobClient;
        private CloudStorageAccount cloudStorageAccount;
        private JsonSerializerSettings jsonSerializerSettings;
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
        public SnapshotStore()
        {
            containerName = "container-x";
            cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            jsonSerializerSettings = SerializerSettings;
        }

        // predpostavimo da container obstaja
        public SnapshotStore(CloudStorageAccount CloudStorageAccount, JsonSerializerSettings JsonSerializerSettings, string ContainerName)
        {
            cloudStorageAccount = CloudStorageAccount;
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            jsonSerializerSettings = JsonSerializerSettings;
            containerName = ContainerName;
        }

        public async Task<SnapshotStream<T>> ProvisonStream<T>(string actorId)
        {
            blobContainer = blobClient.GetContainerReference(containerName);
            await blobContainer.CreateIfNotExistsAsync();
            await blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Container });
            return new SnapshotStream<T>(actorId, blobContainer, SerializerSettings);
        }

    }


}
