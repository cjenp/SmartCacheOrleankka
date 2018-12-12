using System;
using System.Collections.Generic;
using AzureBlobStorage;
using CacheGrainInter;
using Orleankka;
using Orleankka.Meta;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Orleans;

namespace CacheGrainImpl
{

    public class DomainState
    {
        public HashSet<string> Emails = new HashSet<string>();
    }

    public class Domain : EventSourcedActor<DomainState>, IDomain
    {
        public Domain(ISnapshotStore snapshotStore, IEventTableStore eventTableStore, ILogger<DomainState> logger, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(snapshotStore, eventTableStore, logger, id, runtime, dispatcher)
        {
        }

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

    public interface IInventory : IActorGrain, IGrainWithGuidCompoundKey
    { }

    [ImplicitStreamSubscription("CacheGrainInter.IDomain")]
    public class BreachedDomainsList : StreamProjectionActor, IInventory
    {
        readonly Dictionary<string, HashSet<string>> domains =
             new Dictionary<string, HashSet<string>>();

        void On(EventEnvelope<AddedEmailToDomain> e)
        {
            if (!domains.ContainsKey(e.StreamId))
            {
                domains.Add(e.StreamId,new HashSet<string>());
            }
            domains[e.StreamId].Add(e.Event.Email);
        }

        Dictionary<String,int> On(GetDomainsInfo e)
        {
            Dictionary<string, int> domainsInfo = new Dictionary<string, int>();
            foreach(string key in domains.Keys)
            {
                domainsInfo.Add(key, domains[key].Count);
            }
            return domainsInfo;
        }
    }

    public interface IInventoryItemProjection : IActorGrain, IGrainWithGuidCompoundKey
    { }

    [ImplicitStreamSubscription(typeof(StreamFilter))]
    public class InventoryItemProjection : StreamProjectionActor, IInventoryItemProjection
    {
        HashSet<string> emails=new HashSet<string>();

        void On(EventEnvelope<AddedEmailToDomain> e) => emails.Add(e.Event.Email);
        bool On(CheckEmail e) => emails.Contains(e.Email);

    }

    public class StreamFilter : IStreamNamespacePredicate
    {
        public StreamFilter()
        {

        }
        public bool IsMatch(string streamNamespace)
        {
            if (streamNamespace.Contains("CacheGrainInter.IDomain:"))
                return true;
            else
                return false;
        }
    }
}
