namespace Fig.Common.Exceptions
{
    using System.Runtime.Serialization;

    public class FigVersionNotFoundException : FigException
    {
        public FigVersionNotFoundException(string version) : base($"The version '{version}' could not be found, make sure it has been imported.")
        {
        }

        protected FigVersionNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
