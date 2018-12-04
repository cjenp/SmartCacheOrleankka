using AzureBlobStorage;
using FluentAssertions;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        [InlineData("container1",1)]
        [InlineData("container-name",5)]
        [InlineData("bla-nla-bla",99)]
        public async void WriteSnapshotTest(String containerName,int version)
        {
            SnapshotStore bs = new SnapshotStore(CloudStorageAccount.DevelopmentStorageAccount, SerializerSettings);
            String testObj = "Test123";
            String s=await bs.ProvisonStream<String>(containerName).WriteSnapshot(testObj,version);
            s.Should().MatchRegex(String.Format("^http://127\\.0\\.0\\.1:10000/devstoreaccount1/{0}/SNAPSHOT_{1}$", containerName,version));
        }

        [Fact]
        public void WriteAndReadSnapshotTest()
        {
            SnapshotStore bs = new SnapshotStore();
            HashSet<String> testStateStrings=new HashSet<string>();
            testStateStrings.Add("String1");
            testStateStrings.Add("String2");
            testStateStrings.Add("String3");
            testStateStrings.Add("String4");
            testStateStrings.Add("String5");
            var ss = bs.ProvisonStream<HashSet<String>>("emailsi");

            String s = ss.WriteSnapshot(testStateStrings,1).GetAwaiter().GetResult();
            HashSet<String> snap=ss.ReadSnapshotFromUri(s);
            testStateStrings.Should().BeEquivalentTo(snap);


            var ss2 = bs.ProvisonStream<List<DateTime>>("domenasi");
            List<DateTime> testStateDates = new List<DateTime>();
            testStateDates.Add(DateTime.Now);
            testStateDates.Add(new DateTime());

            s = ss2.WriteSnapshot(testStateDates,1).GetAwaiter().GetResult();
            List<DateTime> snapDate = ss2.ReadSnapshotFromUri(s);
            testStateDates.Should().BeEquivalentTo(snapDate);
        }
    }
}
