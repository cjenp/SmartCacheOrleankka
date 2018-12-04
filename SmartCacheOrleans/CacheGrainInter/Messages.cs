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
    public class DomainAddedEmail : Event
    {
        public readonly string Email;

        public DomainAddedEmail(string email)
        {
            Email = email;
        }
    }

    [Serializable]
    public class SnapshotData
    {
        public readonly string SnapshotUri;

        public SnapshotData(string snapshotUri)
        {
            SnapshotUri = snapshotUri;
        }
    }
}
