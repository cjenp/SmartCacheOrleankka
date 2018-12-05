using Newtonsoft.Json;
using Orleankka;
using Orleankka.Meta;
using System;
using System.Collections.Generic;
using System.Globalization;
using Streamstone;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Table;
using AzureBlobStorage;
using CacheGrainInter;

namespace CacheGrainImpl
{

    public abstract class EventSourcedActor<T> : DispatchActorGrain
    {
        public T state;
        ISnapshotStore snapshotStore;
        SnapshotStream<T> snapStream;
        int eventCount = 0;
        int eventsPerSnapshot = 3;

        public EventSourcedActor(ISnapshotStore SnapshotStore,string id = null, IActorRuntime runtime= null, Dispatcher dispatcher= null):base(id, runtime,dispatcher)
        {
            snapshotStore = SnapshotStore;
        }

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

        Stream stream;
        Stream snapShotStream;

        /// <summary>
        /// Executes on grain activation and starts a timer for saving to storage
        /// </summary>
        /// <returns></returns>
        public async override Task OnActivateAsync()
        {
            state = Activator.CreateInstance<T>();
            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        public override async Task<object> Receive(object message)
        {
            switch (message)
            {
                case Activate _:
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

        public async Task LoadSnapshot()
        {
            if (snapStream == null)
            {
                snapStream = await snapshotStore.ProvisonStream<T>(Id);
            }
            var partition = new Partition(SS.Table, SnapshotStreamName());
            var existent = await Stream.TryOpenAsync(partition);
            if (!existent.Found)
            {
                snapShotStream = new Stream(partition);
                return;
            }

            snapShotStream = existent.Stream;
            StreamSlice<EventEntity> slice;

            snapShotStream = await Stream.OpenAsync(partition);
            int vers = snapShotStream.Version;


            if(vers>0)
            {
                slice = await Stream.ReadAsync<EventEntity>(partition, vers, 1);
                var ev = (SnapshotData)DeserializeEvent(slice.Events.Last());
                eventCount = slice.Events.Last().EventsInSnapshot;
                state = snapStream.ReadSnapshotFromUri(ev.SnapshotUri);
            }
            
        }

        async Task Load()
        {
            var partition = new Partition(SS.Table, StreamName());

            var existent = await Stream.TryOpenAsync(partition);
            if (!existent.Found)
            {
                stream = new Stream(partition);
                return;
            }

            stream = existent.Stream;
            StreamSlice<EventEntity> slice;
            var nextSliceStart = eventCount+1;

            do
            {
                slice = await Stream.ReadAsync<EventEntity>(partition, nextSliceStart);

                nextSliceStart = slice.HasEvents
                    ? slice.Events.Last().Version + 1
                    : -1;

                Replay(slice.Events);
                eventCount += slice.Events.Count();
            }
            while (!slice.IsEndOfStream);
        }

        string StreamName()
        {
            return GetType().Name + "-" + Id;
        }

        string SnapshotStreamName()
        {
            return GetType().Name + "-" + Id + "-" +"Snapshot";
        }

        void Replay(IEnumerable<EventEntity> events)
        {
            var deserialized = events.Select(DeserializeEvent).ToArray();
            Apply(deserialized);
        }

        Task<object> HandleQuery(Query query) => Result(Dispatcher.DispatchResult(this, query));

        async Task<object> HandleCommand(Command cmd)
        {
            var events = Dispatcher.DispatchResult<IEnumerable<Event>>(this, cmd).ToArray();
            await Store(events);

            foreach (var @event in events)
            {
                Dispatcher.Dispatch(this, @event);
                eventCount++;
                if (eventCount % eventsPerSnapshot == 0)
                {
                    await CreateSnapshot();
                }
            }
            return Result(events);
        }

        void Apply(IEnumerable<object> events)
        {
            foreach (var @event in events)
                Dispatcher.Dispatch(this, @event);
        }

        async Task Store(Event[] @events)
        {
            if (events == null || @events.Length == 0)
                return;

            var serialized = ToEventData(events);

            try
            {
                var result = await Stream.WriteAsync(stream, serialized);
                stream = result.Stream;
            }
            catch (ConcurrencyConflictException)
            {
                Console.WriteLine("Concurrency conflict on stream '{0}' detected", StreamName());
                Console.WriteLine("Probably, second activation of actor '{0}' has been created", Self);
                Console.WriteLine("Deactivating duplicate activation '{0}' ... ", Self);

                Activation.DeactivateOnIdle();
                throw new InvalidOperationException("Duplicate activation of actor '" + Self + "' detected");
            }
        }

        async Task CreateSnapshot()
        {
            try
            {
               String snapUri = await snapStream.WriteSnapshot(state,eventCount);
               SnapshotData snapshot=new SnapshotData(snapUri);
               EventData[] eventDatas = new EventData[1];

                var id = Guid.NewGuid().ToString("D");

                var properties = new EventEntity
                {
                    Id = id,
                    Type = typeof(SnapshotData).AssemblyQualifiedName,
                    Data = JsonConvert.SerializeObject(snapshot, SerializerSettings),
                    EventsInSnapshot = eventCount
                };
                eventDatas[0] = new EventData(EventId.From(id), EventProperties.From(properties));

                var result = await Stream.WriteAsync(snapShotStream, eventDatas);
                snapShotStream = result.Stream;
            }
            catch (ConcurrencyConflictException)
            {
                Console.WriteLine("Concurrency conflict on stream '{0}' detected", StreamName());
                Console.WriteLine("Probably, second activation of actor '{0}' has been created", Self);
                Console.WriteLine("Deactivating duplicate activation '{0}' ... ", Self);

                Activation.DeactivateOnIdle();
                throw new InvalidOperationException("Duplicate activation of actor '" + Self + "' detected");
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        static object DeserializeEvent(EventEntity @event)
        {
            var eventType = Type.GetType(@event.Type, true);

            Debug.Assert(eventType != null,
                "Couldn't load type '{0}'. Are you missing an assembly reference?", @event.Type);

            return JsonConvert.DeserializeObject(@event.Data, eventType, SerializerSettings);
        }

        static EventData[] ToEventData(Event[] events)
        {
            EventData[] result=new EventData[events.Length];
            for(int i=0;i<events.Length;i++)
            {
                var id = Guid.NewGuid().ToString("D");

                var properties = new EventEntity
                {
                    Id = id,
                    Type = events[i].GetType().AssemblyQualifiedName,
                    Data = JsonConvert.SerializeObject(events[i], SerializerSettings)
                };
                result[i] = new EventData(EventId.From(id), EventProperties.From(properties));
            }

            return result;
        }

        class EventEntity
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Data { get; set; }
            public int Version { get; set; }
            public int EventsInSnapshot { get; set; }
        }
    }

    public static class SS
    {
        public static CloudTable Table
        {
            get; set;
        }
    }
}
