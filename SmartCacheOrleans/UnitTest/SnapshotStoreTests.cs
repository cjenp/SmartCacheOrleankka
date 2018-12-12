using AzureBlobStorage;
using CacheGrainInter;
using FluentAssertions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Orleankka.Meta;
using Serilog;
using Streamstone;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class SnapshotStoreTests
    {
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

        [Theory]
        [InlineData("Stake1",5)]
        [InlineData("State1dsfsdfg", 12)]
        public async Task WriteAndReadSnapshot(String fakeState,int eventsInSnapshot)
        {
            var log = new LoggerConfiguration().CreateLogger();
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            CloudTable cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference("unitTestTable");
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            await cloudTable.DeleteIfExistsAsync();
            await cloudTable.CreateIfNotExistsAsync();

            var blobContainer = blobClient.GetContainerReference("testing");
            await blobContainer.DeleteIfExistsAsync();
            await blobContainer.CreateIfNotExistsAsync();
            await blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Container });

            String streamName = "teststream";
            Partition partition = new Partition(cloudTable, streamName);
            Stream stream = new Stream(partition);

            EventTableStoreStream eventTableStoreStream = new EventTableStoreStream(partition,stream, streamName, SerializerSettings);
            SnapshotBlobStream snapshotBlobStream = new SnapshotBlobStream("snapshot.test",blobContainer,SerializerSettings,eventTableStoreStream);


            await snapshotBlobStream.WriteSnapshot(fakeState, eventsInSnapshot);
            var snapshotData=await snapshotBlobStream.ReadSnapshot();
            String readState = snapshotBlobStream.ReadSnapshotFromUri<String>(snapshotData.SnapshotUri);

            snapshotData.EventsInSNapshot.Should().Be(eventsInSnapshot);
            readState.Should().BeEquivalentTo(fakeState);
        }



        [Theory]
        [MemberData(nameof(TestDataEvents))]
        public async Task WriteAndReadEvents(Event[] events)
        {
            var log = new LoggerConfiguration().CreateLogger();
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            CloudTable cloudTable = cloudStorageAccount.CreateCloudTableClient().GetTableReference("unitTestTable");
            await cloudTable.DeleteIfExistsAsync();
            await cloudTable.CreateIfNotExistsAsync();

            String streamName = "teststream";
            Partition partition = new Partition(cloudTable, streamName);
            Stream stream = new Stream(partition);
            //var events = new Event[] { new DomainAddedEmail("3@domena1.com"), new DomainAddedEmail("2@domena1.com") , new DomainAddedEmail("1@domena1.com") };
            EventTableStoreStream eventTableStoreStream = new EventTableStoreStream(partition,stream,"teststream",SerializerSettings);

            await eventTableStoreStream.StoreEvents(events);
            //var eventsReturned=await eventTableStoreStream.ReadEvents(0);
            int ver = eventTableStoreStream.Version;

            //eventsReturned.Should().BeEquivalentTo(events);
            ver.Should().Be(events.Length);

        }


        public static IEnumerable<object[]> TestDataEvents()
        {
            var testcase = new Event[]
            {
                new AddedEmailToDomain("1@domena1.com"),
                new AddedEmailToDomain("2@domena1.com")
            };
            yield return new object[] { testcase };

            var testcase2 = new Event[]
            {
                new AddedEmailToDomain("1@email.com"),
                new AddedEmailToDomain("2@domena1.com"),
                new AddedEmailToDomain("3@domena1.com"),
                new AddedEmailToDomain("4@domena1.com"),
                new AddedEmailToDomain("5@domena1.com")
            };
            yield return new object[] { testcase };

            var testcase3 = new Event[]
            {
                new AddedEmailToDomain("1@domena1.com"),
                new AddedEmailToDomain("2@domena1.com")
            };
            yield return new object[] { testcase };
        }
    }
}
