using System;
using MetallicBlueDev.EntityGate.InterfacedObject;
using Microsoft.EntityFrameworkCore;

namespace MetallicBlueDev.EntityGate.Core
{
    /// <summary>
    /// Represents the tracking of an entity.
    /// </summary>
    [Serializable()]
    internal class EntityStateTracking
    {
        /// <summary>
        /// Represents the state followed.
        /// </summary>
        internal EntityState State { get; set; }

        /// <summary>
        /// The entity followed.
        /// </summary>
        internal IEntityObjectIdentifier EntityObject { get; private set; }

        /// <summary>
        /// Determine if it is a main entity.
        /// </summary>
        internal bool IsMainEntity { get; private set; }

        /// <summary>
        /// New tracking of an entity.
        /// </summary>
        /// <param name="entityObject">Entity followed.</param>
        /// <param name="state">The initial state.</param>
        /// <param name="isMainEntity">Indicates that it is the main entity.</param>
        internal EntityStateTracking(IEntityObjectIdentifier entityObject, EntityState state, bool isMainEntity)
        {
            IsMainEntity = isMainEntity;
            EntityObject = entityObject;
            State = state;
        }
    }
}

