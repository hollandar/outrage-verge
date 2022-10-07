using System.Runtime.Serialization;

namespace Outrage.Verge.Parser.Tokens
{
    [Serializable]
    internal class AttributeAssertException : Exception
    {
        public AttributeAssertException()
        {
        }

        public AttributeAssertException(string? message) : base(message)
        {
        }

        public AttributeAssertException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected AttributeAssertException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}