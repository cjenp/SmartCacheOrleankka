using CacheGrainInter;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Orleankka.Meta;
using Serilog;
using Serilog.Context;
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
        private int version=-1;
        ILogger log;


        public EventTableStoreStream(CloudTable CloudTable,string StreamName, JsonSerializerSettings SerializerSettings, ILogger Log)
        {
            serializerSettings = SerializerSettings;
            partition = new Partition(CloudTable, StreamName);
            log = Log.ForContext<EventTableStoreStream>();
        }

        private async Task CreateStreamIfNotExists()
        {
            var existent = await Stream.TryOpenAsync(partition);
            stream = existent.Found
                ? existent.Stream : new Stream(partition);
            version = stream.Version;

        }

        public async Task<int> GetVersion()
        {
            await CreateStreamIfNotExists();
            return version;
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
                log.Error(e,"Concurrency conflict detected");
                throw e;
            }
            catch (Exception e)
            {
                log.Error(e,"Exception occured while writing to event store");
                throw e;
            }
        }

        public async Task StoreSnapshot(String uri,int eventCount)
        {
            SnapshotData snapshotData = new SnapshotData(uri,eventCount);
            log.Information("Storing snapshot uri ({SnapshotUri})", uri);
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
                log.Error(e,"Concurrency conflict on stream detected");
                throw e;
            }
            catch(Exception e)
            {
                log.Error(e, "Exception occured while storing snapshot");
                throw e;
            }
        }

        public async Task<IEnumerable<object>> ReadEvents(int lastReadSlice)
        {
            List<object> eventsInStore = new List<object>();
            await CreateStreamIfNotExists();

            if (lastReadSlice > 0)
            {
                try
                {
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
                }
                catch (Exception e)
                {
                    log.Error(e, "Exception occured while reading events");
                    throw e;
                }
            }
            return eventsInStore;
        }

        public async Task<SnapshotData> ReadSnapshot(int snapshotVersion=0)
        {
            await CreateStreamIfNotExists();
            if (snapshotVersion == 0)
            {
                int highestVersion = stream.Version;
                snapshotVersion = highestVersion;
            }
            log.Information("Reading snapshot version {SnapshotVersion}",snapshotVersion);
            StreamSlice<EventEntity> slice;
            try
            {
                if (snapshotVersion > 0)
                {
                    slice = await Stream.ReadAsync<EventEntity>(partition, snapshotVersion, 1);
                    return JsonConvert.DeserializeObject<SnapshotData>(slice.Events.Last().Data, serializerSettings);
                }
                else
                {
                    string errorMsg = String.Format("Cannot read snapshot, none exists!");
                    log.Error(errorMsg);
                    throw new Exception(errorMsg);
                }
            }
            catch (Exception e)
            {
                log.Error(e, "Exception occured while reading snapshot version {SnapshotVersion}", snapshotVersion);
                throw e;
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
            return JsonConvert.DeserializeObject(@event.Data, eventType, serializerSettings);
        }

    }
}