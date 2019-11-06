using System;
using MetallicBlueDev.EntityGate.Core;
using MetallicBlueDev.EntityGate.Helpers;
using MetallicBlueDev.EntityGate.InterfacedObject;
using Microsoft.EntityFrameworkCore;

namespace MetallicBlueDev.EntityGate
{
    /// <summary>
    /// Encapsulates a mapped sql object via its type with context creation automatically.
    /// Allows you to use a raw entity type with a raw context type (DbContext).
    /// Use this if you do not know the type of the entity or the type of context.
    /// </summary>
    /// <remarks>Tip to manage an unknown entity on an unknown context.</remarks>
    [Serializable()]
    public sealed class EntityGateAgent : EntityGateClient<IEntityObjectIdentifier, DbContext>
    {
        /// <summary>
        /// New generic entity manager.
        /// </summary>
        /// <param name="externalEntityType">The type of entity that will be controlled by the manager.</param>
        /// <param name="connectionName">Name of the connection string.</param>
        public EntityGateAgent(Type externalEntityType, string connectionName = null) : base(ReflectionHelper.MakeInstance<IEntityObjectIdentifier>(externalEntityType), connectionName)
        {
        }
    }
}

