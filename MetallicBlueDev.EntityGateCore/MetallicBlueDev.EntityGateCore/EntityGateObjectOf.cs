using System;
using MetallicBlueDev.EntityGate.Core;
using MetallicBlueDev.EntityGate.InterfacedObject;
using Microsoft.EntityFrameworkCore;

namespace MetallicBlueDev.EntityGate
{
    /// <summary>
    /// Encapsulates a mapped sql object.
    /// Allows you to use a specific type of context with a specific entity.
    /// Use this if you know the type of the entity and the type of context.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TContext">The type of context.</typeparam>
    /// <remarks>Tip to force a specific entity on a specific context.</remarks>
    [Serializable()]
    public sealed class EntityGateObject<TEntity, TContext> : EntityGateClient<TEntity, TContext>
      where TEntity : class, IEntityObjectIdentifier
      where TContext : DbContext
    {
        /// <summary>
        /// New entity manager.
        /// </summary>
        /// <param name="externalEntity">External entity that will be controlled by the manager.</param>
        /// <param name="connectionName">Name of the connection string.</param>
        public EntityGateObject(TEntity externalEntity = null, string connectionName = null) : base(externalEntity, connectionName)
        {
        }
    }
}

