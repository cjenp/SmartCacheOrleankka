using System;
using System.Collections.Generic;
using AzureBlobStorage;
using CacheGrainInter;
using Orleankka;
using Orleankka.Meta;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Orleans;
using Microsoft.Extensions.Options;

namespace CacheGrainImpl
{

    public class DomainState
    {
        public HashSet<string> Emails = new HashSet<string>();
    }

    public class DomainsState
    {
        public Dictionary<string, int> Domains = new Dictionary<string, int>();
    }

    public class Domain : EventSourcedActor<DomainState>, IDomain
    {
        public Domain(ISnapshotStore snapshotStore, IEventTableStore eventTableStore, ILogger<Domain> logger, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(snapshotStore, eventTableStore, logger, id, runtime, dispatcher)
        { }

        void On(AddedEmailToDomain e)
        {
            state.Emails.Add(e.Email);
        }

        IEnumerable<Event> Handle(AddEmail cmd)
        {
            if (state.Emails.Contains(cmd.Email))
                throw new EmailConflictException("Email allready exists");

            yield return new AddedEmailToDomain(cmd.Email);
        }

        bool Handle(CheckEmail query)
        {
            return state.Emails.Contains(query.Email);
        }
    }



    [ImplicitStreamSubscription("CacheGrainInter.IDomain")]
    public class BreachedDomains : StreamProjectionActor<DomainsState>, IBreachedDomains
    {
        public BreachedDomains(IOptions<StreamProjectionSettings> streamProjectionSettings, ILogger<BreachedDomains> logger, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(streamProjectionSettings, logger, id, runtime, dispatcher)
        { }

        void On(EventEnvelope<AddedEmailToDomain> e)
        {
            if (!state.Domains.ContainsKey(e.StreamId))
            {
                state.Domains.Add(e.StreamId,0);
            }
            state.Domains[e.StreamId]++;
        }

        Dictionary<String,int> On(GetDomainsInfo e)
        {
            return state.Domains;
        }
    }

    [ImplicitStreamSubscription(typeof(StreamFilter))]
    public class DomainProjection : StreamProjectionActor<DomainState>, IDomainProjection
    {
        public DomainProjection(IOptions<StreamProjectionSettings> streamProjectionSettings, ILogger<DomainProjection> logger, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(streamProjectionSettings, logger, id, runtime, dispatcher)
        { }

        void On(EventEnvelope<AddedEmailToDomain> e) => state.Emails.Add(e.Event.Email);
        bool On(CheckEmail e) => state.Emails.Contains(e.Email);

    }

    public class StreamFilter : IStreamNamespacePredicate
    {
        public StreamFilter()
        { }
        public bool IsMatch(string streamNamespace)
        {
            if (streamNamespace.Contains("CacheGrainInter.IDomain:"))
                return true;
            else
                return false;
        }
    }
}
