using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using MetallicBlueDev.EntityGate.Extensions;
using MetallicBlueDev.EntityGate.GateException;
using MetallicBlueDev.EntityGate.Helpers;
using MetallicBlueDev.EntityGate.InterfacedObject;
using MetallicBlueDev.EntityGateCore.Properties;
using Microsoft.EntityFrameworkCore;

namespace MetallicBlueDev.EntityGate.Core
{
    /// <summary>
    /// Entity tracking management.
    /// </summary>
    [Serializable()]
    internal class EntityGateTracking
    {
        private readonly List<EntityStateTracking> entities = new List<EntityStateTracking>();

        /// <summary>
        /// Unloading empty collections.
        /// To avoid an inconsistent state.
        /// 
        /// TODO This is to be reviewed.
        /// </summary>
        internal void UnloadEmptyEntityCollection()
        {
            foreach (var entityObject in entities.Select(tracking => tracking.EntityObject))
            {
                PocoHelper.SetEmptyEntityCollectionAsNull(entityObject);
            }
        }

        /// <summary>
        /// Returns the main entity followed.
        /// </summary>
        /// <returns></returns>
        internal IEntityObjectIdentifier GetMainEntity()
        {
            var mainEntity = GetMainEntities().ToArray();

            if (mainEntity.Length != 1)
            {
                throw new EntityGateCoreException(Resources.InvalidMainEntity);
            }

            return mainEntity.First();
        }

        /// <summary>
        /// Track the entity with its state.
        /// </summary>
        /// <param name="entity">The entity to follow.</param>
        /// <param name="state">The state to follow.</param>
        /// <param name="isMainEntity">Indicates that it is the main entity.</param>
        internal void MarkEntity(object entity, EntityState state, bool isMainEntity)
        {
            if (!entity.IsEntityObject())
            {
                throw new EntityGateCoreException(string.Format(CultureInfo.CurrentCulture, Resources.MustImplementInterface, entity, nameof(IEntityObjectIdentifier)));
            }

            Mark((IEntityObjectIdentifier)entity, state, isMainEntity);
        }

        /// <summary>
        /// Cleaning the tracking.
        /// </summary>
        internal void CleanTracking()
        {
            entities.Clear();
        }

        /// <summary>
        /// Determines whether entities are followed.
        /// </summary>
        /// <returns></returns>
        internal bool HasEntities()
        {
            return entities.Count > 0;
        }

        /// <summary>
        /// Returns all the followed entities.
        /// </summary>
        /// <returns></returns>
        internal IList<EntityStateTracking> GetEntities()
        {
            return entities;
        }

        /// <summary>
        /// Returns the entities marked as main.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IEntityObjectIdentifier> GetMainEntities()
        {
            return entities
             .Where(tracking => tracking.IsMainEntity)
             .Select(tracking => tracking.EntityObject);
        }

        /// <summary>
        /// Track the entity with its state.
        /// </summary>
        /// <param name="entity">The entity to follow.</param>
        /// <param name="state">The state to follow.</param>
        /// <param name="isMainEntity">Indicates that it is the main entity.</param>
        private void Mark(IEntityObjectIdentifier entity, EntityState state, bool isMainEntity)
        {
            switch (state)
            {
                case EntityState.Deleted:
                case EntityState.Added:
                case EntityState.Modified:
                case EntityState.Unchanged:
                {
                    SetState(entity, state, isMainEntity);
                    break;
                }

                default:
                {
                    throw new EntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidEntityStateForTracking, state, entity));
                }
            }
        }

        /// <summary>
        /// Affects the state to follow.
        /// </summary>
        /// <param name="entity">The entity to follow.</param>
        /// <param name="state">The state to follow.</param>
        /// <param name="isMainEntity">Indicates that it is the main entity.</param>
        private void SetState(IEntityObjectIdentifier entity, EntityState state, bool isMainEntity)
        {
            var trackingIndex = FindEntityIndex(entity);

            if (trackingIndex >= 0)
            {
                entities[trackingIndex].State = state;
            }
            else
            {
                entities.Add(new EntityStateTracking(entity, state, isMainEntity));
            }
        }

        /// <summary>
        /// Returns the index of the entity if it is followed.
        /// </summary>
        /// <param name="entity">The entity followed.</param>
        /// <returns></returns>
        private int FindEntityIndex(IEntityObjectIdentifier entity)
            => entities.FindIndex(tracking => tracking.EntityObject.Equals(entity));
    }
}

