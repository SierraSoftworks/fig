using System;
using System.Runtime.Serialization;

namespace Fig.Common.Exceptions
{
    /// <summary>
    /// An exception thrown by the Fig configuration system.
    /// </summary>
    public class FigException : Exception
    {
        public FigException()
        {
        }

        public FigException(string message) : base(message)
        {
        }

        public FigException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FigException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
