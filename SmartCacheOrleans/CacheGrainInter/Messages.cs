using Orleankka.Meta;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheGrainInter
{
    [Serializable]
    public class AddEmail : Command
    {
        public readonly string Name;

        public AddEmail(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    public class GetDetails : Query<bool>
    { }

    [Serializable]
    public class DomainAddedEmail : Event
    {
        public readonly string Name;

        public DomainAddedEmail(string name)
        {
            Name = name;
        }
    }
}
