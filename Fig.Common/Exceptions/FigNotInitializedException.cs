namespace Fig.Common.Exceptions
{
    using System.Runtime.Serialization;

    public class FigNotInitializedException : FigException
    {
        public FigNotInitializedException() : base($"Your Fig data directory has not yet been initialized.")
        {
        }

        protected FigNotInitializedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
