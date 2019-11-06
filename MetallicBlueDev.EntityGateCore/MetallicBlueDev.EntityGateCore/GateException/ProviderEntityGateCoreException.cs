using System;
using System.Runtime.Serialization;

namespace MetallicBlueDev.EntityGate.GateException
{
    /// <summary>
    /// Internal error to the provider.
    /// </summary>
    [Serializable()]
    public class ProviderEntityGateCoreException : EntityGateCoreException
    {
        /// <summary>
        /// New context error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="inner">Internal error.</param>
        public ProviderEntityGateCoreException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// New context error.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected ProviderEntityGateCoreException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// New context error.
        /// </summary>
        public ProviderEntityGateCoreException() : this(null)
        {
        }

        /// <summary>
        /// New context error.
        /// </summary>
        /// <param name="message">Error message.</param>
        public ProviderEntityGateCoreException(string message) : this(message, null)
        {
        }
    }
}

