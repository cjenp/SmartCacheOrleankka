using System;
using System.Collections.Generic;
using CacheGrainInter;
using Orleankka;
using Orleankka.Meta;
using Orleans;

namespace CacheGrainImpl
{
    
    public class Domain : EventSourcedActor, IDomain
    {
        void On(DomainAddedEmail e)
        {
            emailsCache.Add(e.Email);
        }

        IEnumerable<Event> Handle(AddEmail cmd)
        {
            if (emailsCache.Contains(cmd.Email))
                throw new EmailConflictException("Email allready exists");

            yield return new DomainAddedEmail(cmd.Email);
        }

        bool Handle(CheckEmail query)
        {
            return emailsCache.Contains(query.Email);
        }

    }
}
