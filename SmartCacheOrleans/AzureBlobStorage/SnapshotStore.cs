using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CacheGrainInter;
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
            cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            jsonSerializerSettings = SerializerSettings;
        }

        // predpostavimo da container obstaja
        public SnapshotStore(CloudStorageAccount CloudStorageAccount, JsonSerializerSettings JsonSerializerSettings)
        {
            cloudStorageAccount = CloudStorageAccount;
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            jsonSerializerSettings = JsonSerializerSettings;
        }

        public SnapshotStream<T> ProvisonStream<T>(string actorId)
        {
            return new SnapshotStream<T>(actorId, blobClient, SerializerSettings);
        }

    }


}
