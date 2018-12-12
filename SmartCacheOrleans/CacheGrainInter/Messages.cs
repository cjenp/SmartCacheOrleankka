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

    [Serializable]
    public class EventEnvelope<T> where T : Event
    {
        public readonly string StreamId;
        public readonly T Event;

        public EventEnvelope(string stream, T @event)
        {
            StreamId = stream;
            Event = @event;
        }
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

    public class EventEntity
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public int Version { get; set; }
    }
}
