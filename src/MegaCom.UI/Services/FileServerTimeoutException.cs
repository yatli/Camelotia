using System;
using System.Runtime.Serialization;

namespace Camelotia.Services.Providers
{
    [Serializable]
    internal class FileServerTimeoutException : Exception
    {
        public FileServerTimeoutException()
        {
        }

        public FileServerTimeoutException(string message) : base(message)
        {
        }

        public FileServerTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FileServerTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}