using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobStorage
{
    public interface ISnapshotStore
    {
        Task<SnapshotStream<T>> ProvisonStream<T>(string IdActor);
    }

    public class SnapshotStream<T>
    {
        private CloudBlobContainer blobContainer;
        private JsonSerializerSettings jSSettings;
        private String idActor;

        public SnapshotStream(String IdActor, CloudBlobContainer BlobContainer, JsonSerializerSettings jsonSerializerSettings)
        {
            blobContainer = BlobContainer;
            idActor = IdActor;
            jSSettings = jsonSerializerSettings;
        }

        public async Task<String> WriteSnapshot(T snapshot, int snapshotVersion)
        {
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(String.Format("{0}/Snapshot_{1}", idActor,snapshotVersion));
            string data = JsonConvert.SerializeObject(snapshot);
            using (var stream = new MemoryStream(Encoding.Default.GetBytes(data), false))
            {
                await blob.UploadFromStreamAsync(stream);
            }
            return blob.Uri.AbsoluteUri;
        }

        public T ReadSnapshotFromUri(String uri)
        {
            var webRequest = WebRequest.Create(uri);
            string strContent = String.Empty;

            using (var response = webRequest.GetResponse())
            using (var content = response.GetResponseStream())
            using (var reader = new StreamReader(content))
            {
                strContent = reader.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<T>(strContent, jSSettings);
        }
    }
}