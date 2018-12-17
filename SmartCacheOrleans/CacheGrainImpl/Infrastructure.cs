using AzureBlobStorage;
using CacheGrainInter;
using FileStorageProviderNS;
using Microsoft.Extensions.Logging;
using Orleankka;
using Orleankka.Meta;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheGrainImpl
{
    public abstract class EventSourcedActor<T> : DispatchActorGrain
    {
        protected T state;
        ISnapshotStore snapshotStore;
        ISnapshotBlobStream snapshotBlobStream;

        IEventTableStore eventTableStore;
        IEventTableStoreStream eventTableStoreStream;

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
            state = Activator.CreateInstance<T>();
        }

        public override async Task<object> Receive(object message)
        {
            using (log.BeginScope(loggerScope))
            {
                switch (message)
                {
                    case Activate _:
                        loggerScope.Add("ActorId", Self.Path.Interface);
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

        private async Task CreateStorageAndProjectionStreams()
        {
            streamProjectionAggregate = System.StreamOf("SMSProvider", $"{Self.Path.Interface}");
            streamProjectionDomain = System.StreamOf("SMSProvider", $"{Self.Path}");

            snapshotBlobStream = await snapshotStore.ProvisonSnapshotStream(SnapshotStreamName());
            eventTableStoreStream = await eventTableStore.ProvisonEventStream(StreamName());
        }

        private async Task LoadSnapshot()
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

        private async Task Load()
        {
            var eventsRead = await eventTableStoreStream.ReadEvents(Apply, version);
            version += eventsRead;
            log.LogInformation("Read {NumberOfEvents} events", eventsRead);
        }

        private void Apply(IEnumerable<Event> events)
        {
            foreach (var @event in events)
                Dispatcher.Dispatch(this, @event);
        }

        private Task<object> HandleQuery(Query query)
        {
            log.LogInformation("Handling query {@Query}", query);
            return Result(Dispatcher.DispatchResult(this, query));
        }

        private async Task<object> HandleCommand(Command cmd)
        {

            log.LogInformation("Handling command {@Command}", cmd);
            var events = Dispatcher.DispatchResult<IEnumerable<Event>>(this, cmd).ToArray();
            await eventTableStoreStream.StoreEvents(events);

            Apply(events);
            foreach(Event @event in events)
            {
                version++;
                await Project(@event);
                if (version % eventsPerSnapshot == 0)
                {
                    await snapshotBlobStream.WriteSnapshot(state, version);
                }
            }
            return events;
        }

        private async Task Project(Event @event)
        {
            var envelope = Wrap(@event);
            await streamProjectionAggregate.Push(envelope);
            await streamProjectionDomain.Push(envelope);
        }

        private object Wrap(Event @event)
        {
            var envelopeType = typeof(EventEnvelope<>).MakeGenericType(@event.GetType());
            return Activator.CreateInstance(envelopeType, Id, @event, version);
        }

        private string StreamName()
        {
            return Self.Path;
        }

        private string SnapshotStreamName()
        {
            return GetType().Name + "-" + Id + "-" + "Snapshot";
        }
    }

    public abstract class StreamProjectionAggregateActor<T> : StreamProjectionActor<T>
    {
        Dictionary<string, int> versions = new Dictionary<string, int>();

        public StreamProjectionAggregateActor(IFileStorageProvider fileStorageProvider, IEventTableStore eventTableStore, ILogger log, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(fileStorageProvider, eventTableStore, log, id, runtime, dispatcher)
        { }

        /*
        public override async Task HandleRecivedEvent(object data, StreamSequenceToken token)
        {
            String streamId = (data as dynamic).StreamId;
            int idVersion;
            if(!versions.TryGetValue(streamId, out idVersion))
            {
                versions.Add(streamId, 0);
            }

            if ((data as dynamic).EventVersion == (idVersion + 1))
            {
                await Dispatcher.DispatchAsync(this, data);
                versions[streamId]++;
            }
            else
            {
                EventTableStoreStream eventTableStoreStream = await eventTableStore.ProvisonEventStream(Id);
                var tablePartitions = eventTableStoreStream.GetAllTablePartitions();
                foreach (string partitionName in tablePartitions)
                {
                    await RefreshFromEventStorage(partitionName);
                }
            }
            SaveToFile();
        }

        public async Task RefreshFromEventStorage(String streamId)
        {
            EventTableStoreStream eventTableStoreStream = await eventTableStore.ProvisonEventStream(streamId);
            if (!versions.ContainsKey(streamId))
            {
                versions.Add(streamId, 0);
            }
            versions[streamId] = +await eventTableStoreStream.ReadEvents(Apply, versions[streamId]);
        }*/
    }

    public abstract class StreamProjectionActor<T> : DispatchActorGrain
    {
        protected T state;
        int version=0;
        ILogger log;
        Dictionary<string, object> ActorId = new Dictionary<string, object>();
        protected IEventTableStore eventTableStore;
        protected IFileStorageProvider fileStorageProvider;
        private IEventTableStoreStream eventTableStoreStream;

        public StreamProjectionActor(IFileStorageProvider fileStorageProvider, IEventTableStore eventTableStore , ILogger log, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(id, runtime, dispatcher)
        {
            this.eventTableStore = eventTableStore;
            this.log = log;
            this.fileStorageProvider = fileStorageProvider;
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            eventTableStoreStream = await eventTableStore.ProvisonEventStream(Id);
            ActorId.TryAdd("ProjectionActorId", Self.Path.Id);
            state = Activator.CreateInstance<T>();

            var savedData=await fileStorageProvider.ReadFromFile<T>(Id);
            if (savedData != null)
            {
                state = savedData.State;
                version = savedData.Version;
            }

            var streamProvider = GetStreamProvider("SMSProvider");
            var stream = streamProvider.GetStream<IEventEnvelope>(Guid.Empty, Id);
            await stream.SubscribeAsync(HandleRecivedEvent);

        }

        public virtual async Task HandleRecivedEvent(IEventEnvelope data, StreamSequenceToken token)
        {
            using (log.BeginScope(ActorId))
            {
                if (data.EventVersion == (version+1))
                {
                    await Dispatcher.DispatchAsync(this, data);
                    log.LogInformation("Projection recived event:{eventData}", data);
                    version++;
                }
                else
                {
                    version = +await eventTableStoreStream.ReadEvents(Apply, version);
                }

                fileStorageProvider.SaveToFile<T>(new ProjectionStoreEntity<T>(version,state),Id);
            }
        }
        
        protected void Apply(IEnumerable<Event> events)
        {
            foreach (var @event in events)
                Dispatcher.Dispatch(this, @event);
        }
    } 
}
