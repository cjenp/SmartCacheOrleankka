using CacheGrainInter;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobStorage
{
    public interface ISnapshotStore
    {
        Task<SnapshotBlobStream> ProvisonSnapshotStream(string IdActor);
    }

    public class SnapshotBlobStream
    {
        private CloudBlobContainer blobContainer;
        private JsonSerializerSettings jSSettings;
        private String idActor;
        private EventTableStoreStream eventTableStoreStream;
        private ILogger log;

        public SnapshotBlobStream(String IdActor, CloudBlobContainer BlobContainer, JsonSerializerSettings jsonSerializerSettings, EventTableStoreStream EventTableStoreStream, ILogger Log)
        {
            blobContainer = BlobContainer;
            idActor = IdActor;
            jSSettings = jsonSerializerSettings;
            eventTableStoreStream = EventTableStoreStream;
            log = Log.ForContext<SnapshotBlobStream>();
        }

        public async Task WriteSnapshot(Object snapshot, int eventCount)
        {
            log.Information("Storing snapshot with {EventCount} events", eventCount);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(String.Format("{0}/Snapshot_{1}", idActor, eventCount));
            string data = JsonConvert.SerializeObject(snapshot);
            try
            {
                using (var stream = new MemoryStream(Encoding.Default.GetBytes(data), false))
                {
                    await blob.UploadFromStreamAsync(stream);
                }
            }
            catch(Exception e)
            {
                log.Error(e, "Exception occured while writing snasphot to Blob store");
                throw e;
            }
            var uri=blob.Uri.AbsoluteUri;
            await eventTableStoreStream.StoreSnapshot(uri, eventCount);
        }

        public async Task<int> Version()
        {
            return await eventTableStoreStream.GetVersion();
        }

        public async Task<SnapshotData> ReadSnapshot(int ver=0)
        {
            return await eventTableStoreStream.ReadSnapshot(ver);
        }

        public T ReadSnapshotFromUri<T>(String uri)
        {
            log.Information("Reading snasphot from uri {SnapshotUri}",uri);
            var webRequest = WebRequest.Create(uri);
            string strContent = String.Empty;

            try
            {
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    strContent = reader.ReadToEnd();
                }
            }
            catch(Exception e)
            {
                log.Error(e, "Exception occured while reading snasphot from uri");
                throw e;
            }
            
            return JsonConvert.DeserializeObject<T>(strContent, jSSettings);
        }
    }
}