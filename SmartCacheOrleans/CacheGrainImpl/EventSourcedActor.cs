using Newtonsoft.Json;
using Orleankka;
using Orleankka.Meta;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using AzureBlobStorage;
using CacheGrainInter;
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

        public EventSourcedActor(ISnapshotStore SnapshotStore, IEventTableStore EventTableStore, ILogger Log, string id = null, IActorRuntime runtime= null, Dispatcher dispatcher= null):base(id, runtime,dispatcher)
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
                        log.Information("Grain activated");
                        state = Activator.CreateInstance<T>();
                        await CreateStorageStreams();
                        await LoadSnapshot();
                        await Load();
                        return Done;

                    case Command cmd:
                        return await HandleCommand(cmd);

                    case Query query:
                        return await HandleQuery(query);

                    default:
                        return await base.Receive(message);
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
            if(await snapshotBlobStream.Version() > 0)
            {
                var lastSnapshot= await snapshotBlobStream.ReadSnapshot();         
                version = lastSnapshot.EventsInSNapshot;
                state = snapshotBlobStream.ReadSnapshotFromUri<T>(lastSnapshot.SnapshotUri);
            }
        }

        async Task Load()
        {
            var eventsRead = await eventTableStoreStream.ReadEvents(version);
            version += eventsRead.Count();
            log.Information("Reading {NumberOfEvents} events", eventsRead.Count());
            Apply(eventsRead);

        }

        void Apply(IEnumerable<object> events)
        {
            foreach (var @event in events)
                Dispatcher.Dispatch(this, @event);
        }

        Task<object> HandleQuery(Query query) => Result(Dispatcher.DispatchResult(this, query));

        async Task<object> HandleCommand(Command cmd)
        {
            var events = Dispatcher.DispatchResult<IEnumerable<Event>>(this, cmd).ToArray();
            await eventTableStoreStream.StoreEvents(events);

            foreach (var @event in events)
            {
                log.Information("Handling event \"{EventType}\"", @event.GetType());
                Dispatcher.Dispatch(this, @event);
                version++;
                if (version % eventsPerSnapshot == 0)
                {
                    await snapshotBlobStream.WriteSnapshot(state, version);
                }
            }
            return Result(events);
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
