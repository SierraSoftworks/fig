namespace Fig.Common.Exceptions
{
    using System.Runtime.Serialization;

    public class FigWrongChecksumException : FigException
    {
        public FigWrongChecksumException(string fileName, string expectedHash, string trueHash) : base($"The file [{fileName}] was expected to have a hash of [{expectedHash}] but was found to have a hash of [{trueHash}].")
        {
        }

        protected FigWrongChecksumException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
