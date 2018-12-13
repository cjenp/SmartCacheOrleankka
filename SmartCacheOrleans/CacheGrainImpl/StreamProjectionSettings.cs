namespace CacheGrainImpl
{
    public class StreamProjectionSettings
    {
        public StreamProjectionSettings()
        {

        }
        public StreamProjectionSettings(string fileStoragePath)
        {
            FileStoragePath = fileStoragePath;
        }

        public string FileStoragePath { get; set; }
    }
}
