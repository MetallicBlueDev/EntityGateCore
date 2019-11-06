using System;
using MetallicBlueDev.EntityGate.Core;
using MetallicBlueDev.EntityGate.InterfacedObject;
using Microsoft.EntityFrameworkCore;

namespace MetallicBlueDev.EntityGate
{
    /// <summary>
    /// Encapsulates a mapped sql object with automatic context creation.
    /// Allows you to use a raw context type (DbContext) with a specific entity.
    /// Use this if you know the type of entity but not the type of context.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <remarks>Tip for managing a known entity on an unknown context.</remarks>
    [Serializable()]
    public sealed class EntityGateObject<TEntity> : EntityGateClient<TEntity, DbContext> where TEntity : class, IEntityObjectIdentifier
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

