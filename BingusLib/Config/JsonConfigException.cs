using System.Runtime.Serialization;

namespace BingusLib.Config
{
    [Serializable]
    public class JsonConfigException : Exception
    {
        public JsonConfigException()
        {
        }

        public JsonConfigException(string? message) : base(message)
        {
        }

        public JsonConfigException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected JsonConfigException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
