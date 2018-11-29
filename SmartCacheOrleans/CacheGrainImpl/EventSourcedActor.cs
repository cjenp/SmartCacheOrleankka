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

namespace CacheGrainImpl
{
    public abstract class EventSourcedActor : DispatchActorGrain
    {
        List<Event> eventsCache = new List<Event>();
        bool stateChanged=false;

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

        /// <summary>
        /// Executes on grain activation and starts a timer for saving to storage
        /// </summary>
        /// <returns></returns>
        public override Task OnActivateAsync()
        {
            RegisterTimer(Store, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            return base.OnActivateAsync();
        }

        /// <summary>
        /// Saves grain state before grain deactivates
        /// </summary>
        /// <returns></returns>
        public override Task OnDeactivateAsync()
        {
            Store(null).GetAwaiter().GetResult();
            return base.OnDeactivateAsync();
        }

        public override async Task<object> Receive(object message)
        {
            switch (message)
            {
                case Activate _:
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
            var nextSliceStart = 1;

            do
            {
                slice = await Stream.ReadAsync<EventEntity>(partition, nextSliceStart);

                nextSliceStart = slice.HasEvents
                    ? slice.Events.Last().Version + 1
                    : -1;

                Replay(slice.Events);
            }
            while (!slice.IsEndOfStream);
        }

        string StreamName()
        {
            return GetType().Name + "-" + Id;
        }

        void Replay(IEnumerable<EventEntity> events)
        {
            var deserialized = events.Select(DeserializeEvent).ToArray();
            Apply(deserialized);
        }

        Task<object> HandleQuery(Query query) => Result(Dispatcher.DispatchResult(this, query));

        Task<object> HandleCommand(Command cmd)
        {
            stateChanged = true;
            var events = Dispatcher.DispatchResult<IEnumerable<Event>>(this, cmd).ToArray();
            eventsCache.AddRange(events);
            Apply(events);

            return Result(events);
        }

        void Apply(IEnumerable<object> events)
        {
            foreach (var @event in events)
                Dispatcher.Dispatch(this, @event);
        }

        async Task Store(object _)
        {
            if (eventsCache.Count == 0 || stateChanged==false)
                return;

            var serialized = eventsCache.Select(ToEventData).ToArray();

            try
            {
                var result = await Stream.WriteAsync(stream, serialized);
                stream = result.Stream;
                stateChanged = false;
                eventsCache.Clear();
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

        static object DeserializeEvent(EventEntity @event)
        {
            var eventType = Type.GetType(@event.Type, true);

            Debug.Assert(eventType != null,
                "Couldn't load type '{0}'. Are you missing an assembly reference?", @event.Type);

            return JsonConvert.DeserializeObject(@event.Data, eventType, SerializerSettings);
        }

        static EventData ToEventData(object @event)
        {
            var id = Guid.NewGuid().ToString("D");

            var properties = new EventEntity
            {
                Id = id,
                Type = @event.GetType().AssemblyQualifiedName,
                Data = JsonConvert.SerializeObject(@event, SerializerSettings)
            };

            return new EventData(EventId.From(id), EventProperties.From(properties));
        }

        class EventEntity
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Data { get; set; }
            public int Version { get; set; }
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
