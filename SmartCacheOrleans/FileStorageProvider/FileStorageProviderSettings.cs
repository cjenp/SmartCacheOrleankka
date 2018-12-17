namespace FileStorageProviderNS
{
    public class FileStorageProviderSettings
    {
        public FileStorageProviderSettings()
        {

        }
        public FileStorageProviderSettings(string fileStoragePath)
        {
            FileStoragePath = fileStoragePath;
        }

        public string FileStoragePath { get; set; }
    }
}
