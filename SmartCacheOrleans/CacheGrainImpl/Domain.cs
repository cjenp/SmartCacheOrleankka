using System;
using System.Collections.Generic;
using AzureBlobStorage;
using CacheGrainInter;
using Orleankka;
using Orleankka.Meta;
using Orleans;
using Serilog;

namespace CacheGrainImpl
{

    public  class DomainState
    {
        public HashSet<string> Emails = new HashSet<string>();
    }

    public class Domain : EventSourcedActor<DomainState>, IDomain
    {
        public Domain(ISnapshotStore snapshotStore, IEventTableStore eventTableStore,ILogger log, string id = null, IActorRuntime runtime = null, Dispatcher dispatcher = null) : base(snapshotStore,eventTableStore,log, id, runtime, dispatcher)
        {
        }

        void On(DomainAddedEmail e)
        {
            state.Emails.Add(e.Email);
        }

        IEnumerable<Event> Handle(AddEmail cmd)
        {
            if (state.Emails.Contains(cmd.Email))
                throw new EmailConflictException("Email allready exists");

            yield return new DomainAddedEmail(cmd.Email);
        }

        bool Handle(CheckEmail query)
        {
            return state.Emails.Contains(query.Email);
        }
    }
}
