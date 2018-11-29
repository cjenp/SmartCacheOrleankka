using System;
using System.Collections.Generic;
using System.Text;

namespace CacheGrainInter
{
    public class EmailConflictException : Exception
    {
        public EmailConflictException(string message)
            : base(message)
        {}
    }
}
