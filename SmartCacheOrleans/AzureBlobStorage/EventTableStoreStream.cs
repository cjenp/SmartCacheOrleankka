using CacheGrainInter;
using Newtonsoft.Json;
using Orleankka.Meta;
using Serilog;
using Streamstone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureBlobStorage
{
    public class EventTableStoreStream
    {
        JsonSerializerSettings serializerSettings;
        Partition partition;
        Stream stream;

        public EventTableStoreStream(Partition partition,Stream stream,string StreamName, JsonSerializerSettings SerializerSettings)
        {
            serializerSettings = SerializerSettings;
            this.partition = partition;
            this.stream = stream;
            version = stream.Version;
        }

        private async Task CreateStreamIfNotExists()
        {
            var existent = await Stream.TryOpenAsync(partition);
            stream = existent.Found
                ? existent.Stream : new Stream(partition);
            version = stream.Version;
        }

        private int version;
        public int Version
        {
            get => version;

            private set { }
        }

        public async Task StoreEvents(Event[] events)
        {
            if (events == null || @events.Length == 0)
                return;

            await CreateStreamIfNotExists();
            var serialized = ToEventData(events);
            var result = await Stream.WriteAsync(stream, serialized);
            stream = result.Stream;
            version = stream.Version;
        }

        public async Task StoreSnapshot(String uri,int eventCount)
        {
            SnapshotData snapshotData = new SnapshotData(uri,eventCount);

            await CreateStreamIfNotExists();
            var id = Guid.NewGuid().ToString("D");
            var properties = new EventEntity
            {
                Id = id,
                Type = typeof(SnapshotData).AssemblyQualifiedName,
                Data = JsonConvert.SerializeObject(snapshotData, serializerSettings),
            };
            var eventData = new EventData(EventId.From(id), EventProperties.From(properties));

            var result = await Stream.WriteAsync(stream, eventData);
            stream = result.Stream;
            version = stream.Version;
        }

        public async Task<int> ReadEvents(Action<IEnumerable<Event>> func, int lastReadVersion)
        {
            int eventsRead=0;
            await CreateStreamIfNotExists();

            if (version > 0)
            {
                try
                {
                    StreamSlice<EventEntity> slice;
                    var nextSliceStart = lastReadVersion + 1;
                    do
                    {
                        slice = await Stream.ReadAsync<EventEntity>(partition, nextSliceStart);

                        nextSliceStart = slice.HasEvents
                            ? slice.Events.Last().Version + 1
                            : -1;

                        DeserializeEvent(slice.Events[0]);
                        func(slice.Events.Select(DeserializeEvent));
                        eventsRead += slice.Events.Count();
                    }
                    while (!slice.IsEndOfStream);
                }
                catch (Exception e)
                {
                    throw new EventTableStoreStreamException("Exception occured while reading events", e);
                }
            }
            return eventsRead;
        }

        public async Task<SnapshotData> ReadSnapshot(int snapshotVersion=0)
        {
            await CreateStreamIfNotExists();
            if (snapshotVersion == 0)
            {
                int highestVersion = stream.Version;
                snapshotVersion = highestVersion;
            }

            StreamSlice<EventEntity> slice;
        
                if (snapshotVersion > 0)
                {
                    slice = await Stream.ReadAsync<EventEntity>(partition, snapshotVersion, 1);
                    return JsonConvert.DeserializeObject<SnapshotData>(slice.Events.Last().Data, serializerSettings);
                }
                else
                {
                    throw new EventTableStoreStreamException("Cannot read snapshot, none exists!");
                }
            
        }

        private EventData[] ToEventData(Event[] events)
        {
            EventData[] result = new EventData[events.Length];
            for (int i = 0; i < events.Length; i++)
            {
                var id = Guid.NewGuid().ToString("D");

                var properties = new EventEntity
                {
                    Id = id,
                    Type = events[i].GetType().AssemblyQualifiedName,
                    Data = JsonConvert.SerializeObject(events[i], serializerSettings)
                };
                result[i] = new EventData(EventId.From(id), EventProperties.From(properties));
            }

            return result;
        }

        Event DeserializeEvent(EventEntity @event)
        {
            var eventType = Type.GetType(@event.Type, true);
            return (Event)JsonConvert.DeserializeObject(@event.Data,eventType,serializerSettings);
        }

    }
}