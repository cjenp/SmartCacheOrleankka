using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace AzureBlobStorage
{
    public class SnapshotStore<T>
    {
        private CloudBlobContainer blobContainer;
        private CloudBlobClient blobClient;
        private CloudStorageAccount cloudStorageAccount;
        private JsonSerializerSettings jsonSerializerSettings;

        private int lastSnapshotVer = 0;
        private T lastSnapshot;

        // predpostavimo da container obstaja
        public SnapshotStore()
        {
            cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference("container");
        }

        // predpostavimo da container obstaja
        public SnapshotStore(CloudStorageAccount CloudStorageAccount,String ContainerName, JsonSerializerSettings JsonSerializerSettings)
        {
            cloudStorageAccount = CloudStorageAccount;
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference(ContainerName);
            jsonSerializerSettings = JsonSerializerSettings;
        }

        public async Task WriteSnapshot(T snapshot)
        {
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference("eventsblob1");
            string data = JsonConvert.SerializeObject(snapshot);
            using (var stream = new MemoryStream(Encoding.Default.GetBytes(data), false))
            {
               await blob.UploadFromStreamAsync(stream);
            }
        }

        public async Task<List<IListBlobItem>> ReadLastSnapshot()
        {
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            do
            {
                var response = await blobContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
                    
            }
            while (continuationToken != null);
            return results;
        }
    }


}
