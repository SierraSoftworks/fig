namespace Fig.Common.Exceptions
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public class FigUnrecognizedKindException : FigException
    {
        public FigUnrecognizedKindException(string family, string kind, IEnumerable<string> options) : base($"You have not yet specified a known {family} kind, got '{kind}' but expected one of [{string.Join(", ", options)}].")
        {
        }

        protected FigUnrecognizedKindException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
