using Orleankka;
using Orleankka.Meta;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AzureBlobStorage;
using Serilog;
using Serilog.Context;

namespace CacheGrainImpl
{
    public abstract class EventSourcedActor<T> : DispatchActorGrain
    {
        protected T state;
        ISnapshotStore snapshotStore;
        SnapshotBlobStream snapshotBlobStream;

        IEventTableStore eventTableStore;
        EventTableStoreStream eventTableStoreStream;

        int version = 0;
        int eventsPerSnapshot = 3;

        protected ILogger log;

        public EventSourcedActor(ISnapshotStore SnapshotStore, IEventTableStore EventTableStore, ILogger Log, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(id, runtime, dispatcher)
        {
            snapshotStore = SnapshotStore;
            eventTableStore = EventTableStore;
            log = Log.ForContext<EventSourcedActor<T>>();
        }

        public override async Task<object> Receive(object message)
        {
            using (LogContext.PushProperty("GranID", Id))
            {
                switch (message)
                {
                    case Activate _:
                        await CreateStorageStreams();
                        await LoadSnapshot();
                        await Load();

                        log.Information("Grain activated");
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

        public async Task CreateStorageStreams()
        {
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
            log.Information("Read {NumberOfEvents} events", eventsRead);
        }

        void Apply(IEnumerable<Event> events)
        {
            foreach (var @event in events)
                Dispatcher.Dispatch(this, @event);
        }

        Task<object> HandleQuery(Query query)
        {
            log.Information("Handling query {@Query}", query);
            return Result(Dispatcher.DispatchResult(this, query));
        }

        async Task<object> HandleCommand(Command cmd)
        {
            log.Information("Handling command {@Command}", cmd);
            var events = Dispatcher.DispatchResult<IEnumerable<Event>>(this, cmd).ToArray();
            await eventTableStoreStream.StoreEvents(events);

            Apply(events);
            version += events.Count();
            if (version % eventsPerSnapshot > 0)
            {
                await snapshotBlobStream.WriteSnapshot(state, version);
            }

            return events;
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
}
