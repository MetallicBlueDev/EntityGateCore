using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MetallicBlueDev.EntityGate.GateException;
using MetallicBlueDev.EntityGate.InterfacedObject;
using MetallicBlueDev.EntityGateCore.Properties;
using Microsoft.EntityFrameworkCore;

namespace MetallicBlueDev.EntityGate.Helpers
{
    /// <summary>
    /// Help with the manipulation of entities.
    /// </summary>
    public static class EntityHelper
    {
        /// <summary>
        /// Returns the handler for the requested entity.
        /// </summary>
        /// <typeparam name="TContext">The type of context.</typeparam>
        /// <param name="component">An instance of an entity.</param>
        /// <returns></returns>
        public static EntityGateContext<TContext> GetEntityGate<TContext>(ref IEntityObjectIdentifier component) where TContext : DbContext
        {
            var gate = new EntityGateContext<TContext>(component);

            if (!gate.IsNewEntity)
            {
                gate.Load();
            }

            component = gate.Entity;

            return gate;
        }

        /// <summary>
        /// Returns the handler for the requested entity.
        /// </summary>
        /// <typeparam name="TContext">The type of context.</typeparam>
        /// <param name="entityType">The type of an entity known to the context.</param>
        /// <param name="entityIdentifier">The key of the entity.</param>
        /// <returns></returns>
        public static EntityGateContext<TContext> GetEntityGate<TContext>(Type entityType, object entityIdentifier) where TContext : DbContext
        {
            var speedEntityInstance = ReflectionHelper.MakeInstance<IEntityObjectIdentifier>(entityType);
            var gate = new EntityGateContext<TContext>(speedEntityInstance);

            if (!gate.Load(entityIdentifier))
            {
                throw new EntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToLoadEntityKey, gate.GetFriendlyName(), entityIdentifier != null ? entityIdentifier.ToString() : "null"));
            }

            return gate;
        }

        /// <summary>
        /// Returns the typed entity after checking its compatibility.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entity">The entity to check.</param>
        /// <returns></returns>
        public static TEntity CheckEntityType<TEntity>(IEntityObjectIdentifier entity) where TEntity : class, IEntityObjectIdentifier
        {
            if (entity == null || typeof(TEntity) != entity.GetType())
            {
                throw new EntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidEntityType, entity != null ? entity.GetType().ToString() : "null"));
            }

            return (TEntity)entity;
        }

        /// <summary>
        /// Retrieve all the contents of a table as an entity list.
        /// Use with moderation, loss of performance.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <returns></returns>
        public static List<TEntity> LoadAllEntities<TEntity>() where TEntity : class, IEntityObjectIdentifier
        {
            return new EntityGateObject<TEntity>().List().ToList();
        }
    }
}

