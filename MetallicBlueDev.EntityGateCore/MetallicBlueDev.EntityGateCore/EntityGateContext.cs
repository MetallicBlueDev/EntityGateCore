using System;
using MetallicBlueDev.EntityGate.Core;
using MetallicBlueDev.EntityGate.InterfacedObject;
using Microsoft.EntityFrameworkCore;

namespace MetallicBlueDev.EntityGate
{
    /// <summary>
    /// Encapsulates a mapped sql object using the targeted context.
    /// Allows you to use a raw entity type with a specific context.
    /// Use this if you know the type of context but not the type of the entity.
    /// </summary>
    /// <typeparam name="TContext">The type of context.</typeparam>
    /// <remarks>Tip to manage an unknown entity on a known context.</remarks>
    [Serializable()]
    public sealed class EntityGateContext<TContext> : EntityGateClient<IEntityObjectIdentifier, TContext> where TContext : DbContext
    {
        /// <summary>
        /// New entity manager.
        /// </summary>
        /// <param name="externalEntity">External entity that will be controlled by the manager.</param>
        /// <param name="connectionName">Name of the connection string.</param>
        public EntityGateContext(IEntityObjectIdentifier externalEntity = null, string connectionName = null) : base(externalEntity, connectionName)
        {
        }

        /// <summary>
        /// Context builder for entities.
        /// </summary>
        /// <param name="connectionName">Name of the connection string.</param>
        /// <returns></returns>
        public static TContext MakeModel(string connectionName = null)
        {
            var gate = new EntityGateObject<IEntityObjectIdentifier, TContext>(null, connectionName);
            return gate.GetContext();
        }
    }
}

