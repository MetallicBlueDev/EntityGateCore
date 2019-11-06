using System;
using System.Globalization;
using System.Text;
using MetallicBlueDev.EntityGate.GateException;
using MetallicBlueDev.EntityGate.Helpers;
using MetallicBlueDev.EntityGate.InterfacedObject;
using MetallicBlueDev.EntityGateCore.Properties;

namespace MetallicBlueDev.EntityGate.Extensions
{
    /// <summary>
    /// Help with the manipulation of <see cref="IEntityObjectIdentifier"/>.
    /// </summary>
    public static class EntityObjectExtensions
    {
        /// <summary>
        /// Returns a conform copy of the entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="pocoEntity">The entity to clone. Do not put a proxy entity.</param>
        /// <param name="withDataRelation">With or without relationships (level 1 or higher).</param>
        /// <returns></returns>
        public static TEntity CloneEntity<TEntity>(this TEntity pocoEntity, bool withDataRelation = false) where TEntity : class, IEntityObjectIdentifier
        {
            TEntity result = null;

            if (pocoEntity != null)
            {
                result = ReflectionHelper.CloneEntity(pocoEntity, pocoEntity.GetType(), withDataRelation);
            }

            return result;
        }

        /// <summary>
        /// Load or search the requested entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entity">The instance of the entity.</param>
        /// <returns></returns>
        public static TEntity Reload<TEntity>(this TEntity entity) where TEntity : class, IEntityObjectIdentifier
        {
            TEntity result = null;

            if (entity != null)
            {
                var gate = new EntityGateObject<TEntity>(entity);

                if (!gate.Load())
                {
                    throw new EntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToLoadEntityKey, gate.GetFriendlyName(), entity.Identifier));
                }

                result = gate.Entity;
            }

            return result;
        }

        /// <summary>
        /// Returns the entity's information as a string.
        /// </summary>
        /// <param name="entity">The instance of the entity.</param>
        /// <returns></returns>
        public static string GetContentInfo(this IEntityObjectIdentifier entity)
        {
            var content = new StringBuilder();

            if (entity != null)
            {
                content.Append(entity);
                content.Append(" (");

                ReflectionHelper.BuiltContentInfo(entity, content);

                content.Append(")");
            }
            else
            {
                content.Append("Entity is null");
            }

            return content.ToString();
        }

        /// <summary>
        /// Determines if the key appears valid.
        /// </summary>
        /// <param name="entity">The instance of the entity.</param>
        /// <returns></returns>
        public static bool HasValidEntityKey(this IEntityObjectIdentifier entity)
        {
            var valid = false;

            if (entity != null)
            {
                valid = entity.Identifier != null && !entity.Identifier.Equals(null);

                if (valid && entity.Identifier.GetType().IsPrimitive)
                {
                    valid = Convert.ToInt64(entity.Identifier, CultureInfo.InvariantCulture) > 0;
                }
            }

            return valid;
        }

        /// <summary>
        /// Determines whether the entity should feed a history (<see cref="IEntityObjectArchival"/>).
        /// </summary>
        /// <param name="entityObject">The instance of the entity.</param>
        /// <returns></returns>
        public static bool IsEntityArchival(this IEntityObjectIdentifier entityObject)
        {
            return entityObject is IEntityObjectArchival;
        }

        /// <summary>
        /// Determines whether the entity has a specific name (<see cref="IEntityObjectNameable"/>).
        /// </summary>
        /// <param name="entityObject">The instance of the entity.</param>
        /// <returns></returns>
        public static bool IsEntityNameable(this IEntityObjectIdentifier entityObject)
        {
            return entityObject is IEntityObjectNameable;
        }

        /// <summary>
        /// Determines whether the entity has an identification code (<see cref="IEntityObjectRecognizableCode"/>).
        /// </summary>
        /// <param name="entityObject">The instance of the entity.</param>
        /// <returns></returns>
        public static bool HasEntityRecognizableCode(this IEntityObjectIdentifier entityObject)
        {
            return entityObject is IEntityObjectRecognizableCode;
        }

        /// <summary>
        /// Determines whether the entity has a unique value (<see cref="IEntityObjectSingleValue"/>).
        /// </summary>
        /// <param name="entityObject">The instance of the entity.</param>
        /// <returns></returns>
        public static bool HasEntitySingleValue(this IEntityObjectIdentifier entityObject)
        {
            return entityObject is IEntityObjectSingleValue;
        }

        /// <summary>
        /// Returns the specific name of the entity (<see cref="IEntityObjectNameable"/>).
        /// </summary>
        /// <param name="entityObject">The instance of the entity.</param>
        /// <returns></returns>
        public static string GetEntityName(this IEntityObjectIdentifier entityObject)
        {
            string name = null;

            if (entityObject.IsEntityNameable())
            {
                name = ((IEntityObjectNameable)entityObject)?.Name;
            }

            return name;
        }

        /// <summary>
        /// Returns the code of the entity (<see cref="IEntityObjectRecognizableCode"/>).
        /// </summary>
        /// <param name="entityObject">The instance of the entity.</param>
        /// <returns></returns>
        public static string GetEntityCodeName(this IEntityObjectIdentifier entityObject)
        {
            string name = null;

            if (entityObject.HasEntityRecognizableCode())
            {
                name = ((IEntityObjectRecognizableCode)entityObject)?.CodeName;
            }

            return name;
        }

        /// <summary>
        /// Returns the unique value of the entity (<see cref="IEntityObjectSingleValue"/>).
        /// </summary>
        /// <param name="entityObject">The instance of the entity.</param>
        /// <returns></returns>
        public static string GetEntitySingleValue(this IEntityObjectIdentifier entityObject)
        {
            string name = null;

            if (entityObject.HasEntitySingleValue())
            {
                name = ((IEntityObjectSingleValue)entityObject)?.SingleValue;
            }

            return name;
        }

        /// <summary>
        /// Determines whether the entity type appears valid.
        /// </summary>
        /// <param name="entity">The instance of the entity.</param>
        /// <returns></returns>
        public static bool IsValidEntityType(this IEntityObjectIdentifier entity)
        {
            return entity != null
                && ReflectionHelper.IsRealType(entity.GetType());
        }
    }
}

