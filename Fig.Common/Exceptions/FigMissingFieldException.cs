namespace Fig.Common.Exceptions
{
    using System.Runtime.Serialization;

    public class FigMissingFieldException : FigException
    {
        public FigMissingFieldException(string fieldName) : base($"You have not specified a value for the required '{fieldName}' field.")
        {
        }

        protected FigMissingFieldException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
