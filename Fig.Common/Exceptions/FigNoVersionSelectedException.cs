namespace Fig.Common.Exceptions
{
    using System.Runtime.Serialization;

    public class FigNoVersionSelectedException : FigException
    {
        public FigNoVersionSelectedException() : base($"You have not yet selected a configuration version to use with Fig.")
        {
        }

        protected FigNoVersionSelectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
