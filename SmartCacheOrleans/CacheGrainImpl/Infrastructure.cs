using Orleankka;
using Orleankka.Meta;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AzureBlobStorage;
using Microsoft.Extensions.Logging;
using CacheGrainInter;
using Orleans.Streams;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace CacheGrainImpl
{
    public abstract class EventSourcedActor<T> : DispatchActorGrain
    {
        protected T state;
        ISnapshotStore snapshotStore;
        SnapshotBlobStream snapshotBlobStream;

        IEventTableStore eventTableStore;
        EventTableStoreStream eventTableStoreStream;

        StreamRef streamProjectionAggregate;
        StreamRef streamProjectionDomain;

        int version = 0;
        int eventsPerSnapshot = 3;

        protected ILogger log;
        Dictionary<string, object> loggerScope = new Dictionary<string, object>();

        public EventSourcedActor(ISnapshotStore SnapshotStore, IEventTableStore EventTableStore, ILogger log, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(id, runtime, dispatcher)
        {
            snapshotStore = SnapshotStore;
            eventTableStore = EventTableStore;
            this.log = log;

        }

        public override async Task<object> Receive(object message)
        {
            if (!loggerScope.ContainsKey("ActorId"))
                loggerScope.Add("ActorId", Self.Path.Interface);

            using (log.BeginScope(loggerScope))
            {
                switch (message)
                {
                    case Activate _:
                        await CreateStorageAndProjectionStreams();
                        await LoadSnapshot();
                        await Load();
                        log.LogInformation("Grain activated");
                        return Done;

                    case LifecycleMessage lm:
                        return await base.Receive(lm);

                    case Command cmd:
                        return await HandleCommand(cmd);

                    case Query query:
                        return await HandleQuery(query);

                    default:
                        throw new ApplicationException($"Not supported message {message} received");
                }
            }
        }

        public async Task CreateStorageAndProjectionStreams()
        {
            streamProjectionAggregate = System.StreamOf("SMSProvider", $"{Self.Path.Interface}");
            streamProjectionDomain = System.StreamOf("SMSProvider", $"{Self.Path}");

            snapshotBlobStream = await snapshotStore.ProvisonSnapshotStream(SnapshotStreamName());
            eventTableStoreStream = await eventTableStore.ProvisonEventStream(StreamName());
        }

        public async Task LoadSnapshot()
        {
            if (snapshotBlobStream.Version() > 0)
            {
                var lastSnapshot = await snapshotBlobStream.ReadSnapshot();
                version = lastSnapshot.EventsInSNapshot;
                state = snapshotBlobStream.ReadSnapshotFromUri<T>(lastSnapshot.SnapshotUri);
            }
            else
                state = Activator.CreateInstance<T>();
        }

        async Task Load()
        {
            var eventsRead = await eventTableStoreStream.ReadEvents(Apply, version);
            version += eventsRead;
            log.LogInformation("Read {NumberOfEvents} events", eventsRead);
        }

        void Apply(IEnumerable<Event> events)
        {
            foreach (var @event in events)
                Dispatcher.Dispatch(this, @event);
        }

        Task<object> HandleQuery(Query query)
        {
            log.LogInformation("Handling query {@Query}", query);
            return Result(Dispatcher.DispatchResult(this, query));
        }

        async Task<object> HandleCommand(Command cmd)
        {

            log.LogInformation("Handling command {@Command}", cmd);
            var events = Dispatcher.DispatchResult<IEnumerable<Event>>(this, cmd).ToArray();
            await eventTableStoreStream.StoreEvents(events);

            Apply(events);
            version += events.Count();
            if (version % eventsPerSnapshot > 0)
            {
                await snapshotBlobStream.WriteSnapshot(state, version);
            }
            await Project(events);
            return events;
        }

        async Task Project(IEnumerable<Event> events)
        {
            foreach (var @event in events)
            {
                var envelope = Wrap(@event);
                await streamProjectionAggregate.Push(envelope);
                await streamProjectionDomain.Push(envelope);
            }
        }

        object Wrap(Event @event)
        {
            var envelopeType = typeof(EventEnvelope<>).MakeGenericType(@event.GetType());
            return Activator.CreateInstance(envelopeType, Id, @event);
        }

        string StreamName()
        {
            return GetType().Name + "-" + Id;
        }

        string SnapshotStreamName()
        {
            return GetType().Name + "-" + Id + "-" + "Snapshot";
        }
    }

    public abstract class StreamProjectionActor<T> : DispatchActorGrain
    {
        protected T state;
        ILogger log;
        Dictionary<string, object> loggerScope = new Dictionary<string, object>();
        string fileStoragePath;
        string fileStorageFolder;

        public StreamProjectionActor(IOptions<StreamProjectionSettings> streamProjectionSettings, ILogger log, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(id, runtime, dispatcher)
        {
            this.log = log;
            fileStorageFolder = streamProjectionSettings.Value.FileStoragePath;
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            loggerScope.Add("ProjectionActorId", Self.Path.Id);
            fileStoragePath = $"{fileStorageFolder}\\{Id.Replace(":", "_")}.json";
            state = Activator.CreateInstance<T>();
            await ReadFromFile();
            var streamProvider = GetStreamProvider("SMSProvider");
            var stream = streamProvider.GetStream<object>(Guid.Empty, Id);
            await stream.SubscribeAsync(HandleRecivedEvent);

        }

        public async Task HandleRecivedEvent(object data, StreamSequenceToken token)
        {
            using (log.BeginScope(loggerScope))
            {
                await Dispatcher.DispatchAsync(this, data);
                log.LogInformation("Projection recived event:{eventData}", data);
                SaveToFile();
            }
        }

        public void SaveToFile()
        {
            using (StreamWriter streamWriter = new StreamWriter(fileStoragePath))
            {
                JsonSerializer jsonSerializer = new JsonSerializer();
                jsonSerializer.Serialize(streamWriter, state);
            }
        }

        public async Task ReadFromFile()
        {
            if (File.Exists(fileStoragePath))
            {
                String serializedState = string.Empty;
                using (StreamReader streamReader = new StreamReader(fileStoragePath))
                {
                    do
                    {
                        char[] buffer = new char[1000];
                        int readCount = await streamReader.ReadBlockAsync(buffer);
                        serializedState += new string(buffer.Take(readCount).ToArray());
                    } while (!streamReader.EndOfStream);
                    state = JsonConvert.DeserializeObject<T>(serializedState);
                }
            }
        }
    } 
}
