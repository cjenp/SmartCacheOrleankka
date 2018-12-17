using AzureBlobStorage;
using CacheGrainImpl;
using CacheGrainInter;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Orleankka.Meta;
using Orleankka.TestKit;
using System;
using System.Globalization;
using Orleankka;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTest
{
    public class EventSourcedActorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async void EventSourcedActorActivationTest(int snapshotVersion)
        {
            var logger = Mock.Of<ILogger<Domain>>();
            var mockSnapshotBlobStore = new Mock<ISnapshotStore>();
            var mockEventTableStore = new Mock<IEventTableStore>();

            var mockSnapshotBlobStoreStream = new Mock<ISnapshotBlobStream>();
            var mockEventTableStoreStream = new Mock<IEventTableStoreStream>();

            mockSnapshotBlobStore.Setup(x => x.ProvisonSnapshotStream(It.IsAny<String>()))
                .ReturnsAsync(mockSnapshotBlobStoreStream.Object);

            mockEventTableStore.Setup(x => x.ProvisonEventStream(It.IsAny<String>()))
                .ReturnsAsync(mockEventTableStoreStream.Object);

            var fakeSnapshot = Task.FromResult(new SnapshotData("", 1));
            mockSnapshotBlobStoreStream.Setup(x => x.Version()).Returns(snapshotVersion);
            mockSnapshotBlobStoreStream.Setup(x => x.ReadSnapshot(It.IsAny<int>()))
                .Returns(fakeSnapshot);

            Domain domain = new Domain(mockSnapshotBlobStore.Object, mockEventTableStore.Object, logger, "id",
                new ActorRuntimeMock());

            await domain.Receive(new Activate());
            mockEventTableStoreStream.Verify(
                mock => mock.ReadEvents(It.IsAny<Action<IEnumerable<Event>>>(), It.IsAny<int>()), Times.Exactly((1)));

            if (snapshotVersion > 0)
                mockSnapshotBlobStoreStream.Verify(mock => mock.ReadSnapshot(It.IsAny<int>()), Times.Exactly((1)));
            else
                mockSnapshotBlobStoreStream.Verify(mock => mock.ReadSnapshot(It.IsAny<int>()), Times.Exactly((0)));
        }


        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(12)]
        [InlineData(50)]
        public async void WriteEventsAndSnapshotTest(int numOfEvents)
        {
            var logger = Mock.Of<ILogger<Domain>>();
            var mockSnapshotBlobStore = new Mock<ISnapshotStore>();
            var mockEventTableStore = new Mock<IEventTableStore>();

            var mockSnapshotBlobStoreStream = new Mock<ISnapshotBlobStream>();
            var mockEventTableStoreStream = new Mock<IEventTableStoreStream>();

            mockSnapshotBlobStore.Setup(x => x.ProvisonSnapshotStream(It.IsAny<String>()))
                .ReturnsAsync(mockSnapshotBlobStoreStream.Object);

            mockEventTableStore.Setup(x => x.ProvisonEventStream(It.IsAny<String>()))
                .ReturnsAsync(mockEventTableStoreStream.Object);


            Domain domain = new Domain(mockSnapshotBlobStore.Object, mockEventTableStore.Object, logger, "id",
                new ActorRuntimeMock());

            await domain.Receive(new Activate());

            for (int i = 0; i < numOfEvents; i++)
            {
                await domain.Receive(new AddEmail($"{i}@email.si"));
            }

            await domain.Receive(new CheckEmail("nejc@nejc.com"));

            int shouldBeSnapshots = (int) Math.Floor(numOfEvents / 3.0);

            mockEventTableStoreStream.Verify(mock => mock.StoreEvents(It.IsAny<Event[]>()),
                Times.Exactly((numOfEvents)));
            mockSnapshotBlobStoreStream.Verify(mock => mock.WriteSnapshot(It.IsAny<object>(), It.IsAny<int>()),
                Times.Exactly(shouldBeSnapshots));
        }
    }
}

