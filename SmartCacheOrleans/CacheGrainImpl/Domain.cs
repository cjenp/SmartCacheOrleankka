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
        HashSet<String> emails=new HashSet<String>();
        void On(DomainAddedEmail e)
        {
            emails.Add(e.Email);
        }

        IEnumerable<Event> Handle(AddEmail cmd)
        {
            if (emails.Contains(cmd.Email))
                throw new EmailConflictException("Email allready exists");

            yield return new DomainAddedEmail(cmd.Email);
        }

        bool Handle(CheckEmail query)
        {
            return emails.Contains(query.Email);
        }

    }
}
