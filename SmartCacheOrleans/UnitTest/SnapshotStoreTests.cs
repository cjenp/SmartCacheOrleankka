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

        [Fact]
        public void WriteSnapshotTest()
        {
            /*
            BreachedEmailStore bs = new BreachedEmailStore(CloudStorageAccount.DevelopmentStorageAccount, SerializerSettings,"UnitTest1","UnitTestTable");
            String testObj = "Test123";
            var snasphotStream=await bs.ProvisonSnapshotStream(blobFolderName);
            String s=await snasphotStream.WriteSnapshot(testObj, version);

            s.Should().MatchRegex(String.Format("^http://127\\.0\\.0\\.1:10000/devstoreaccount1/{0}/SNAPSHOT_{1}$", blobFolderName, version));*/
        }

        [Fact]
        public void WriteAndReadSnapshotTest()
        {
            /*
            BreachedEmailStore bs = new BreachedEmailStore();
            HashSet<String> testStateStrings=new HashSet<string>();
            testStateStrings.Add("String1");
            testStateStrings.Add("String2");
            testStateStrings.Add("String3");
            testStateStrings.Add("String4");
            testStateStrings.Add("String5");
            var ss = await bs.ProvisonSnapshotStream("emailsi");

            String s = await ss.WriteSnapshot(testStateStrings,1);
            HashSet<String> snap=ss.ReadSnapshotFromUri<HashSet<String>>(s);
            testStateStrings.Should().BeEquivalentTo(snap);


            var ss2 = await bs.ProvisonSnapshotStream("domenasi");
            List<DateTime> testStateDates = new List<DateTime>();
            testStateDates.Add(DateTime.Now);
            testStateDates.Add(new DateTime());

            s = ss2.WriteSnapshot(testStateDates,1).GetAwaiter().GetResult();
            List<DateTime> snapDate = ss2.ReadSnapshotFromUri< List<DateTime> > (s);
            testStateDates.Should().BeEquivalentTo(snapDate);*/
        }
    }
}
