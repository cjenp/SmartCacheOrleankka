using Orleankka.Meta;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheGrainInter
{
    [Serializable]
    public class AddEmail : Command
    {
        public readonly string Email;

        public AddEmail(string email)
        {
            Email = email;
        }
    }

    [Serializable]
    public class GetEventsLog : Command
    {
        public readonly int Version;

        public GetEventsLog(int version)
        {
            Version = version;
        }
    }
    

    [Serializable]
    public class CheckEmail : Query<bool>
    {
        public readonly string Email;
        public CheckEmail(string email)
        {
            Email = email;
        }

    }

    [Serializable]
    public class GetDomainsInfo : Query<Dictionary<String,int>>
    {}

    [Serializable]
    public class AddedEmailToDomain : Event
    {
        public readonly string Email;

        public AddedEmailToDomain(string email)
        {
            Email = email;
        }
    }

    public interface IEventEnvelope
    {
        string StreamId { get; set; }
        int EventVersion { get; set; }

    }
    public interface IEventEnvelope<T>: IEventEnvelope where T : Event
    {
        T Event { get; set; }

    }
    [Serializable]
    public class EventEnvelope<T>: IEventEnvelope<T> where T : Event
    {

        public EventEnvelope(string stream, T @event, int eventVersion)
        {
            StreamId = stream;
            Event = @event;
            EventVersion = eventVersion;
        }

     public   string StreamId { get; set; }
        public int EventVersion { get; set; }
        public T Event { get; set; }

    }

    [Serializable]
    public class SnapshotData
    {
        public readonly string SnapshotUri;
        public readonly int EventsInSNapshot;

        public SnapshotData(string snapshotUri, int eventsInSNapshot)
        {
            SnapshotUri = snapshotUri;
            EventsInSNapshot = eventsInSNapshot;
        }
    }

    [Serializable]
    public class ProjectionStoreEntity<T>
    {
        public int Version;
        public T State;

        public ProjectionStoreEntity(int version, T state)
        {
            Version = version;
            State = state;
        }
    }

    public class EventEntity
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public int Version { get; set; }
    }
}
