using System;
using System.Collections.Generic;
using MetallicBlueDev.EntityGate.Configuration;
using MetallicBlueDev.EntityGate.Core;
using MetallicBlueDev.EntityGate.InterfacedObject;

namespace MetallicBlueDev.EntityGate
{
    /// <summary>
    /// Encapsulates a mapped sql object in its context.
    /// Allows interaction with the entity and its context without knowing precisely the type of manager.
    /// </summary>
    public interface IEntityGateObject : IDisposable
    {
        /// <summary>
        /// Returns the token related to the model.
        /// </summary>
        EntityGateToken Token { get; }

        /// <summary>
        /// Obtain the SQL configuration.
        /// </summary>
        ClientConfiguration Configuration { get; }

        /// <summary>
        /// Returns the current entity.
        /// </summary>
        IEntityObjectIdentifier CurrentEntityObject { get; }

        /// <summary>
        /// Determines if there is at least one managed entity.
        /// </summary>
        bool HasEntityObject { get; }

        /// <summary>
        /// Determines whether the entity has not been added to the database.
        /// </summary>
        bool IsNewEntity { get; }

        /// <summary>
        /// Request the creation of a new entity.
        /// </summary>
        void NewEntity();

        /// <summary>
        /// Returns the name of the table containing the entity.
        /// </summary>
        /// <returns></returns>
        string GetTableName();

        /// <summary>
        /// Returns the name of the entity in the case of the <see cref="IEntityObjectNameable"/> interface implementation, otherwise the primary key with its value.
        /// </summary>
        /// <returns>Returns an identifying information of the object.</returns>
        string GetFriendlyName();

        /// <summary>
        /// Returns the primary key (column name and value).
        /// </summary>
        /// <returns></returns>
        KeyValuePair<string, object> GetPrimaryKey();

        /// <summary>
        /// Load the entity.
        /// </summary>
        /// <param name="identifier">The value of the key.</param>
        /// <returns></returns>
        bool Load(object identifier = null);

        /// <summary>
        /// Saving the entity.
        /// </summary>
        /// <returns></returns>
        bool Save();

        /// <summary>
        /// Deleting the entity.
        /// </summary>
        /// <returns></returns>
        bool Delete();

        /// <summary>
        /// Mark the entity for deletion.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <returns>Returns the instance of the POCO entity.</returns>
        IEntityObjectIdentifier Delete(IEntityObjectIdentifier entity);

        /// <summary>
        /// Applies the entity in the context of the main entity.
        /// In the case of a known context (EntityGateContext), if there is no main entity, the entity in parameter will assume the role of "main" entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>Returns the instance of the POCO entity.</returns>
        IEntityObjectIdentifier Apply(IEntityObjectIdentifier entity);

        /// <summary>
        /// Returns the stored source values.
        /// Calling this function on a new entity will return a null value.
        /// </summary>
        /// <param name="allProperties">Return all properties.</param>
        /// <returns></returns>
        KeyValuePair<string, object>[] GetOriginalValues(bool allProperties = false);

        /// <summary>
        /// Returns the value of the requested field.
        /// </summary>
        /// <param name="fieldName">Name of the column.</param>
        /// <returns></returns>
        object GetFieldValue(string fieldName);

        /// <summary>
        /// Load and return the list of entities with the requested type.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IEntityObjectIdentifier> ListEntities();

        /// <summary>
        /// Change the entity to manage.
        /// Be careful, you must be sure that your manager can manage the type of the entity.
        /// Can generate exceptions if you do not respect the currently managed entity type.
        ///
        /// If your manager uses a generic entity type, then it can accept any known entity from your current context (DAL).
        /// In the case where your manager uses a specific type of entity, then it will be imperative to provide the same type of entity.
        /// </summary>
        /// <param name="entity">The entity to manage.</param>
        void SetEntityObject(IEntityObjectIdentifier entity);
    }
}

