using System.Runtime.Serialization;

namespace BingusLib.Config
{
    [Serializable]
    public class BingusConfigException : Exception
    {
        public BingusConfigException()
        {
        }

        public BingusConfigException(string? message) : base(message)
        {
        }

        public BingusConfigException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BingusConfigException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
