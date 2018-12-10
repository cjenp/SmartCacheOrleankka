using System;
using System.Collections.Generic;
using System.Text;

namespace AzureBlobStorage
{
    class EventTableStoreStreamException: Exception
    {
        public EventTableStoreStreamException(string message): base(message)
        { }

        public EventTableStoreStreamException(string message, Exception innerException): base(message, innerException)
        { }
    }

    class SnapshotBlobStreamException : Exception
    {
        public SnapshotBlobStreamException(string message): base(message)
        { }

        public SnapshotBlobStreamException(string message,Exception innerException): base(message, innerException)
        { }
    }
}
