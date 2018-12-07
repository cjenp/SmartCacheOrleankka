namespace AzureBlobStorage
{
    public class EventTableStoreSettings
    {
        public EventTableStoreSettings()
        {

        }
        public EventTableStoreSettings(string azureConnectionString, string tableName)
        {
            AzureConnectionString = azureConnectionString;
            TableName = tableName;
        }

        public string AzureConnectionString { get; set; }
        public string TableName { get; set; }
    }
}