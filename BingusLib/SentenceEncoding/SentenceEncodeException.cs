using System.Runtime.Serialization;

namespace BingusLib.SentenceEncoding
{
    public class SentenceEncodeException : Exception
    {
        public SentenceEncodeException() { }

        public SentenceEncodeException(string? message)
            : base(message) { }

        public SentenceEncodeException(string? message, Exception? innerException)
            : base(message, innerException) { }
    }
}
