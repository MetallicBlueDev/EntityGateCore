using System;
using System.Runtime.Serialization;

namespace MetallicBlueDev.EntityGate.GateException
{
    /// <summary>
    /// Transaction cancellation (may be automatically triggered internally or externally).
    /// </summary>
    [Serializable()]
    public class CanceledEntityGateCoreException : EntityGateCoreException
    {
        /// <summary>
        /// New transaction cancellation.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="inner">Internal error.</param>
        public CanceledEntityGateCoreException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// New transaction cancellation.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected CanceledEntityGateCoreException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// New transaction cancellation.
        /// </summary>
        public CanceledEntityGateCoreException() : this(null)
        {
        }

        /// <summary>
        /// New transaction cancellation.
        /// </summary>
        /// <param name="message">Error message.</param>
        public CanceledEntityGateCoreException(string message) : this(message, null)
        {
        }
    }
}

