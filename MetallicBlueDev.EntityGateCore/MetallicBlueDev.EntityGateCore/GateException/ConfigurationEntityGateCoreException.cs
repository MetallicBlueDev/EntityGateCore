using System;
using System.Runtime.Serialization;

namespace MetallicBlueDev.EntityGate.GateException
{
    /// <summary>
    /// An error occurred in the configuration.
    /// </summary>
    [Serializable()]
    public class ConfigurationEntityGateCoreException : EntityGateCoreException
    {
        /// <summary>
        /// New configuration error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="inner">Internal error.</param>
        public ConfigurationEntityGateCoreException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// New configuration error.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected ConfigurationEntityGateCoreException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// New configuration error.
        /// </summary>
        public ConfigurationEntityGateCoreException() : this(null)
        {
        }

        /// <summary>
        /// New configuration error.
        /// </summary>
        /// <param name="message">Error message.</param>
        public ConfigurationEntityGateCoreException(string message) : this(message, null)
        {
        }
    }
}

