namespace AzureBlobStorage
{
    public class SnapshotBlobStoreSettings
    {
        public SnapshotBlobStoreSettings()
        {

        }
        public SnapshotBlobStoreSettings(string azureConnectionString, string containerName, string tableName)
        {
            AzureConnectionString = azureConnectionString;
            ContainerName = containerName;
            TableName = tableName;
        }

        public string AzureConnectionString { get; set; }
        public string ContainerName { get; set; }
        public string TableName { get; set; }
    }
}