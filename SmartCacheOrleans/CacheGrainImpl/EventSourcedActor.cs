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

        public EventSourcedActor(ISnapshotStore SnapshotStore, IEventTableStore EventTableStore, ILogger<T> log, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(id, runtime, dispatcher)
        {
            snapshotStore = SnapshotStore;
            eventTableStore = EventTableStore;
            this.log = log;
        }

        public override async Task<object> Receive(object message)
        {

            var dic = new Dictionary<string, object>()
            {
                ["ActorId"] = Self.Path.Id,
            };

            using (log.BeginScope(dic))
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

    public abstract class StreamProjectionActor : DispatchActorGrain
    {
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            var streamProvider = GetStreamProvider("SMSProvider");
            var stream = streamProvider.GetStream<object>(Guid.Empty, Id);
            await stream.SubscribeAsync(async (data, token) =>
                await Dispatcher.DispatchAsync(this, data)
            );
        }
    }
}
