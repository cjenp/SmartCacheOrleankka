using CacheGrainInter;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Orleankka.Meta;
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
        public int Version=-1;

        public EventTableStoreStream(CloudTable CloudTable,string StreamName, JsonSerializerSettings SerializerSettings)
        {
            serializerSettings = SerializerSettings;
            partition = new Partition(CloudTable, StreamName);
        }

        private async Task<bool> CreateStreamIfNotExists()
        {
            var existent = await Stream.TryOpenAsync(partition);
            if (!existent.Found)
            {
                stream = new Stream(partition);
                Version = stream.Version;
                return false;
            }
            else
            {
                stream = existent.Stream;
                Version = stream.Version;
                return true;
            }
        }

        public async Task StoreEvents(Event[] events)
        {
            if (events == null || @events.Length == 0)
                return;

            await CreateStreamIfNotExists();
            var serialized = ToEventData(events);

            try
            {
                var result = await Stream.WriteAsync(stream, serialized);
                stream = result.Stream;
            }
            catch (ConcurrencyConflictException e)
            {
                //Console.WriteLine("Concurrency conflict on stream '{0}' detected", StreamName());
                //Console.WriteLine("Probably, second activation of actor '{0}' has been created", Self);
                //Console.WriteLine("Deactivating duplicate activation '{0}' ... ", Self);
                throw e;
                //throw new InvalidOperationException("Duplicate activation of actor '" + Self + "' detected");
            }
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

            try
            {
                var result = await Stream.WriteAsync(stream, eventData);
                stream = result.Stream;
            }
            catch (ConcurrencyConflictException e)
            {
                //Console.WriteLine("Concurrency conflict on stream '{0}' detected", StreamName());
                //Console.WriteLine("Probably, second activation of actor '{0}' has been created", Self);
                //Console.WriteLine("Deactivating duplicate activation '{0}' ... ", Self);
                throw e;
                //throw new InvalidOperationException("Duplicate activation of actor '" + Self + "' detected");
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        public async Task<IEnumerable<object>> ReadEvents(int lastReadSlice)
        {
            List<object> eventsInStore = new List<object>();
            if (!await CreateStreamIfNotExists())
                return eventsInStore;

            StreamSlice<EventEntity> slice;
            var nextSliceStart = lastReadSlice + 1;
            do
            {
                slice = await Stream.ReadAsync<EventEntity>(partition, nextSliceStart);

                nextSliceStart = slice.HasEvents
                    ? slice.Events.Last().Version + 1
                    : -1;

                eventsInStore.AddRange(slice.Events.Select(DeserializeEvent));
            }
            while (!slice.IsEndOfStream);
            return eventsInStore;
        }

        public async Task<SnapshotData> ReadSnapshot(int ver=0)
        {
            await CreateStreamIfNotExists();
            if (ver == 0)
            {
                int highestVersion = stream.Version;
                ver = highestVersion;
            }
                
            StreamSlice<EventEntity> slice;
            if (ver > 0)
            {
                slice = await Stream.ReadAsync<EventEntity>(partition, ver, 1);
                return JsonConvert.DeserializeObject<SnapshotData>(slice.Events.Last().Data, serializerSettings);
            }
            else
            {
                throw new Exception("No snapshot found!");
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

        object DeserializeEvent(EventEntity @event)
        {
            var eventType = Type.GetType(@event.Type, true);

            //Debug.Assert(eventType != null,
            //    "Couldn't load type '{0}'. Are you missing an assembly reference?", @event.Type);

            return JsonConvert.DeserializeObject(@event.Data, eventType, serializerSettings);
        }

    }
}