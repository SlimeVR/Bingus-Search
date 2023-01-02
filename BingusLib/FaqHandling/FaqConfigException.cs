using System.Runtime.Serialization;

namespace BingusLib.FaqHandling
{
    [Serializable]
    public class FaqConfigException : Exception
    {
        public FaqConfigException()
        {
        }

        public FaqConfigException(string? message) : base(message)
        {
        }

        public FaqConfigException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected FaqConfigException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
