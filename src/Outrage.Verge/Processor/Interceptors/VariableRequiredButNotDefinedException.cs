using System.Runtime.Serialization;

namespace Outrage.Verge.Processor.Interceptors
{
    [Serializable]
    internal class VariableRequiredButNotDefinedException : Exception
    {
        public VariableRequiredButNotDefinedException()
        {
        }

        public VariableRequiredButNotDefinedException(string? message) : base(message)
        {
        }

        public VariableRequiredButNotDefinedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected VariableRequiredButNotDefinedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}