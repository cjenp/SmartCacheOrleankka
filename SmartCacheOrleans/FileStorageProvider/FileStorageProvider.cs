using CacheGrainInter;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileStorageProviderNS
{
    public interface IFileStorageProvider
    {
        void SaveToFile<T>(ProjectionStoreEntity<T> itemToStore, String grainId);
        Task<ProjectionStoreEntity<T>> ReadFromFile<T>(String grainId);
    }

    public class FileStorageProvider: IFileStorageProvider
    {
        private string FileStoragePath;
        public FileStorageProvider(IOptions<FileStorageProviderSettings> fileStorageProviderSettings)
        {
            FileStoragePath = fileStorageProviderSettings.Value.FileStoragePath;
        }

        public void SaveToFile<T>(ProjectionStoreEntity<T> itemToStore, String grainId)
        {
            var fileName = $"{FileStoragePath}\\{grainId.Replace(":", "_")}.json";
            using (StreamWriter streamWriter = new StreamWriter(fileName))
            {
                JsonSerializer jsonSerializer = new JsonSerializer();
                jsonSerializer.Serialize(streamWriter, itemToStore);
            }
        }

        public async Task<ProjectionStoreEntity<T>> ReadFromFile<T>(String grainId)
        {
            var fileName = $"{FileStoragePath}\\{grainId.Replace(":", "_")}.json";
            ProjectionStoreEntity<T> storedItem=null;
            if (File.Exists(FileStoragePath))
            {
                String serializedState = string.Empty;
                using (StreamReader streamReader = new StreamReader(fileName))
                {
                    do
                    {
                        char[] buffer = new char[1000];
                        int readCount = await streamReader.ReadBlockAsync(buffer);
                        serializedState += new string(buffer.Take(readCount).ToArray());
                    } while (!streamReader.EndOfStream);
                    storedItem = JsonConvert.DeserializeObject<ProjectionStoreEntity<T>>(serializedState);
                }
            }
            return storedItem;
        }
    }
}
