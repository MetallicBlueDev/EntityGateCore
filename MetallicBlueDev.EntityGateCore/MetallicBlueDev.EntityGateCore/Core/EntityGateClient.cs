using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using MetallicBlueDev.EntityGate.Configuration;
using MetallicBlueDev.EntityGate.Extensions;
using MetallicBlueDev.EntityGate.GateException;
using MetallicBlueDev.EntityGate.Helpers;
using MetallicBlueDev.EntityGate.InterfacedObject;
using MetallicBlueDev.EntityGateCore.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MetallicBlueDev.EntityGate.Core
{
    /// <summary>
    /// POCO Entities (Plain Old CLR Object).
    /// 
    /// Encapsulates a mapped sql object.
    /// Internal mechanics of the manager.
    /// </summary>
    [Serializable()]
    public abstract class EntityGateClient<TEntity, TContext> : IEntityGateObject
      where TEntity : class, IEntityObjectIdentifier
      where TContext : DbContext
    {
        private const string NewKeyValue = "NewKey";

        private IDictionary<string, object> primaryKeys = null;
        private KeyValuePair<string, object>[] originalValues = null;

        private EntityGateProvider<TContext> provider = null;
        private bool disposed = false;

        [NonSerialized()]
        private TEntity entity = null;

        /// <inheritdoc />
        public EntityGateToken Token { get; }

        /// <inheritdoc />
        public ClientConfiguration Configuration { get; private set; }

        /// <summary>
        /// Get or set the entity.
        /// </summary>
        public TEntity Entity
        {
            get => GetOrCreateEntityObject();
            set => SetEntityObject(value);
        }

        /// <summary>
        /// Determines if there is at least one managed entity.
        /// </summary>
        public bool HasEntityObject => entity != null;

        /// <summary>
        /// Determines whether the entity has not been added to the database.
        /// </summary>
        /// <value></value>
        public bool IsNewEntity => !entity.HasValidEntityKey();

        /// <summary>
        /// Returns the current entity.
        ///
        /// Use the <see cref="Entity"/> property if your handler is strongly typed.
        /// </summary>
        public IEntityObjectIdentifier CurrentEntityObject => Entity;

        /// <summary>
        /// Dynamic creation of the Sql object wrapper.
        /// The type of the entity or the type of context may be unknown, but not both at the same time.
        /// </summary>
        /// <param name="externalEntity">External entity that will be controlled by the manager.</param>
        /// <param name="connectionName">Name of the connection string.</param>
        internal EntityGateClient(TEntity externalEntity = null, string connectionName = null)
        {
            Token = new EntityGateToken();
            Configuration = new ClientConfiguration();

            if (connectionName.IsNotNullOrEmpty())
            {
                Configuration.ChangeConnectionString(connectionName);
            }

            entity = externalEntity;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!disposed)
            {
                FreeMemory();
                disposed = true;
            }
        }

        /// <summary>
        /// Request the creation of a new entity.
        /// </summary>
        public void NewEntity()
        {
            Token.AllowedSaving = true;
            ExecutionStart();

            try
            {
                MakeEntityObject();
            }
            catch (Exception ex)
            {
                LogNewEntityError();
                this.LogException(ex);
                throw;
            }
            finally
            {
                ExecutionEnd();
            }
        }

        /// <summary>
        /// Change the entity to manage.
        /// Be careful, you must be sure that your manager can manage the type of the entity.
        /// Can generate exceptions if you do not respect the currently managed entity type.
        ///
        /// If your manager uses a generic entity type, then it can accept any known entity from your current context (DAL).
        /// In the case where your manager uses a specific type of entity, then it will be imperative to provide the same type of entity.
        /// Prefer to use the <see cref="Entity"/> property if your handler is strongly typed.
        /// </summary>
        /// <param name="entity">The instance of the entity to manage.</param>
        public void SetEntityObject(IEntityObjectIdentifier entity)
        {
            ExecutionStart();
            FireSetEntityObject(entity);
        }

        /// <summary>
        /// Load the entity.
        /// </summary>
        /// <param name="identifier">The value of the key.</param>
        /// <returns></returns>
        public bool Load(object identifier = null)
        {
            var loaded = false;
            Token.AllowedSaving = false;
            ExecutionStart();
            LogLoad(identifier);

            try
            {
                loaded = LoadEntity(GetEntityIdentifier(identifier));
            }
            catch (Exception ex)
            {
                LogLoadError();
                this.LogException(ex);
                throw;
            }
            finally
            {
                ExecutionEnd();
            }

            return loaded;
        }

        /// <summary>
        /// Performs a listing of the entity.
        ///
        /// Equivalent of a SELECT * FROM MyTable.
        /// Use with moderation, loss of performance.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TEntity> List()
        {
            Token.AllowedSaving = false;
            ExecutionStart();
            LogList();

            IEnumerable<TEntity> rslt = null;

            try
            {
                rslt = LoadEntitySet();
            }
            catch (Exception ex)
            {
                LogListError();
                this.LogException(ex);
                throw;
            }
            finally
            {
                ExecutionEnd();
            }

            return rslt;
        }

        /// <summary>
        /// Load and return the list of entities with the requested type.
        /// 
        /// Use <see cref="List"/> if your handler is strongly typed.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEntityObjectIdentifier> ListEntities()
        {
            return List();
        }

        /// <summary>
        /// Saving the entity.
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            Token.AllowedSaving = true;
            ExecutionStart();
            LogSave();

            try
            {
                SaveEntity();
                ManageNotification();
            }
            catch (Exception ex)
            {
                LogSaveError();
                this.LogException(ex);
                throw;
            }
            finally
            {
                ExecutionEnd();
            }

            return Token.NumberOfRows > 0;
        }

        /// <summary>
        /// Deleting the entity.
        /// </summary>
        /// <returns></returns>
        public bool Delete()
        {
            Delete(entity);
            return Save();
        }

        /// <summary>
        /// Mark the entity for deletion.
        /// Returns the instance of the POCO entity.
        /// </summary>
        /// <param name="entity">The instance of the entity to manage.</param>
        public IEntityObjectIdentifier Delete(IEntityObjectIdentifier entity)
        {
            ExecutionStart();

            if (!HasEntityType() && !provider.HasPocoEntityType())
            {
                FireSetEntityObject(entity);
                entity = this.entity;
            }

            return MarkAs(entity, EntityState.Deleted);
        }

        /// <summary>
        /// Applies the entity in the context of the main entity.
        /// Returns the instance of the POCO entity.
        /// 
        /// In the case of a known context (EntityGateContext), if there is no main entity, the entity in parameter will assume the role of "main" entity.
        /// Mark the entity as added or modified.
        /// </summary>
        /// <param name="entity">The instance of the entity to manage.</param>
        public IEntityObjectIdentifier Apply(IEntityObjectIdentifier entity)
        {
            ExecutionStart();

            if (!HasEntityType() && !provider.HasPocoEntityType())
            {
                FireSetEntityObject(entity);
            }
            else
            {
                entity = entity.HasValidEntityKey() ? MarkAs(entity, EntityState.Modified) : MarkAs(entity, EntityState.Added);
            }

            return entity;
        }

        /// <summary>
        /// Returns the stored source values.
        /// 
        /// Calling this function on a new entity will return a <code>null</code> value.
        /// </summary>
        /// <param name="allProperties">Return all properties.</param>
        /// <returns></returns>
        public KeyValuePair<string, object>[] GetOriginalValues(bool allProperties = false)
        {
            var currentOriginalValues = originalValues;

            if (currentOriginalValues == null && provider != null)
            {
                currentOriginalValues = IsNewEntity ? Array.Empty<KeyValuePair<string, object>>() : provider.GetOriginalValues(entity, allProperties);

                if (Token.SaveOriginalValues)
                {
                    originalValues = currentOriginalValues;
                }
            }

            return currentOriginalValues;
        }

        /// <summary>
        /// Returns the value of the requested field.
        /// </summary>
        /// <param name="fieldName">Name of the column.</param>
        /// <returns></returns>
        public object GetFieldValue(string fieldName)
        {
            object value = null;

            if (HasEntityObject && provider != null && provider.HasPocoEntityType())
            {
                value = PocoHelper.GetFieldValue(entity, provider.GetPocoEntityType(), fieldName);
            }

            return value;
        }

        /// <summary>
        /// Returns the name of the table containing the entity.
        /// </summary>
        /// <returns></returns>
        public string GetTableName()
        {
            var name = provider != null && provider.HasPocoEntityType()
                ? provider.GetPocoEntityType().Name
                : HasEntityObject ? entity.GetType().Name : typeof(TEntity).Name;
            return name;
        }

        /// <summary>
        /// Returns the name of the entity in the case of the <see cref="IEntityObjectNameable"/> interface implementation, otherwise the primary key with its value.
        /// </summary>
        /// <returns>Returns an identifying information of the object.</returns>
        public string GetFriendlyName()
        {
            string fName;

            if (HasEntityObject)
            {
                fName = entity.GetEntityName();

                // Using the primary key.
                if (fName == null)
                {
                    fName = GetPrimaryKeyFriendlyName();
                }
            }
            else
            {
                fName = "Virtual entity of " + GetTableName();
            }

            return fName;
        }

        /// <summary>
        /// Returns the primary key (column name and value).
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<string, object> GetPrimaryKey()
        {
            var propKey = default(KeyValuePair<string, object>);

            if (HasEntityObject)
            {
                propKey = GetPrimaryKeys().FirstOrDefault();

                if (propKey.Key == null)
                {
                    LogNoKeyFound();
                }
            }

            return propKey;
        }

        /// <summary>
        /// Returns the list of primary keys.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, object>> GetPrimaryKeys()
        {
            if (primaryKeys == null && HasEntityObject)
            {
                var keyNames = provider.GetPrimaryKeys(entity);
                var primaryKeys = new Dictionary<string, object>();

                foreach (var keyName in keyNames)
                {
                    primaryKeys.Add(keyName, GetFieldValue(keyName));
                }

                if (this.primaryKeys.Count < 1)
                {
                    LogEntityKeyNotFound(null);
                }
            }

            return primaryKeys;
        }

        /// <summary>
        /// Returns the current context.
        /// </summary>
        /// <returns></returns>
        internal TContext GetContext()
        {
            ExecutionStart();

            // Stop automatic loading.
            provider.ChangeLazyLoading(false);

            // Indicates that the context is no longer under control.
            provider.NoTracking();

            return provider.GetContext();
        }

        /// <summary>
        /// Preparation method before serialization.
        /// </summary>
        /// <param name="context"></param>
        [OnSerializing()]
        protected void OnSerializing(StreamingContext context)
        {
            if (provider != null)
            {
                if (HasEntityObject && !provider.HasEntity(entity))
                {
                    AppendEntity(entity);
                }

                provider.ManagePocoEntitiesTracking();
                CheckAutoSaveOriginalValues();
            }
        }

        /// <summary>
        /// Initialization method after deserialization.
        /// </summary>
        /// <param name="context"></param>
        [OnDeserialized()]
        protected void OnDeserialized(StreamingContext context)
        {
            if (provider != null)
            {
                entity = provider.GetMainEntity<TEntity>();
            }

            CheckProvider();
        }

        /// <summary>
        /// Signals the beginning of the execution of the query.
        /// </summary>
        private void ExecutionStart()
        {
            Token.NumberOfAttempts = 0;
            Token.NumberOfRows = -1;

            CheckProvider();
        }

        /// <summary>
        /// Signals the end of the execution of the request.
        /// </summary>
        private void ExecutionEnd()
        {
            provider.CleanTracking();

            CleanOriginalValues();
        }

        /// <summary>
        /// Publication of the events.
        /// </summary>
        private void PublishEvents()
        {
            foreach (var tracking in provider.GetChangedEntries())
            {
                var gate = new EntityGateObject<IEntityObjectIdentifier>(tracking.EntityObject);
                gate.NoTracking();
            }
        }

        /// <summary>
        /// Routine executed during the assignment of the new provider.
        /// </summary>
        private void OnProviderAffected()
        {
            var saveEntity = entity;

            // Reset so that the system attaches the entity itself.
            entity = null;

            // Initialization of the entity and its context.
            if (saveEntity.IsValidEntityType())
            {
                // Early learning of the entity in the new context (deserialization).
                AppendEntity(saveEntity);
            }
        }

        /// <summary>
        /// Procedure for cleaning up used resources.
        /// </summary>
        private void FreeMemory()
        {
            if (provider != null)
            {
                provider.Dispose();
                provider = null;
            }

            CleanOriginalValues();
            CleanPrimaryKeys();
        }

        /// <summary>
        /// Determines if a new execution is possible.
        /// </summary>
        /// <returns></returns>
        private bool ExecutionAllowed()
        {
            return Token.NumberOfAttempts < Configuration.MaximumNumberOfAttempts;
        }

        /// <summary>
        /// Determines if a new execution is possible.
        /// </summary>
        /// <returns></returns>
        private bool ExecutionAllowed(Exception ex)
        {
            return ExecutionAllowed() && !ExceptionHelper.IsInvalidQuery(ex);
        }

        /// <summary>
        /// Determines whether it is possible to publish an event.
        /// </summary>
        /// <returns></returns>
        private bool CanPublishEvent()
        {
            return Configuration.CanUseNotification
                  && Token.AllowedSaving
                  && Token.NumberOfRows > 0;
        }

        /// <summary>
        /// Creation and assignment of the provider instance.
        /// </summary>
        private void MakeProvider()
        {
            provider = GetNewProvider();
            provider.Initialize();

            OnProviderAffected();
        }

        /// <summary>
        /// Returns a new data provider.
        /// </summary>
        /// <returns></returns>
        /// <inheritdoc />
        private EntityGateProvider<TContext> GetNewProvider()
        {
            var newProvider = new EntityGateProvider<TContext>(this);

            if (HasEntityType())
            {
                newProvider.SetPocoEntityType(typeof(TEntity));
            }
            else if (HasEntityObject)
            {
                newProvider.SetPocoEntityType(entity.GetType());
            }

            return newProvider;
        }

        /// <summary>
        /// Verification of the provider's status.
        /// </summary>
        private void CheckProvider()
        {
            if (disposed)
            {
                throw new CanceledEntityGateCoreException(Resources.ObjectDisposed);
            }

            if (provider == null)
            {
                MakeProvider();
            }
            else
            {
                provider.Initialize();
            }
        }

        /// <summary>
        /// Preparation of the execution.
        /// </summary>
        private void PreparingExecution()
        {
            if (Token.NumberOfAttempts > 0)
            {
                Thread.Sleep(Configuration.AttemptDelay);
                LogNextAttempt();
            }

            // Execution counter.
            Token.NumberOfAttempts += 1;
        }

        /// <summary>
        /// Event management.
        /// </summary>
        private void ManageNotification()
        {
            if (CanPublishEvent())
            {
                PublishEvents();
            }
        }

        /// <summary>
        /// Print in the log the new attempt.
        /// </summary>
        private void LogNextAttempt()
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Warning))
            {
                Configuration.Logger.LogWarning(string.Format(CultureInfo.InvariantCulture, Resources.NewAttempt, Token.NumberOfAttempts, Configuration.MaximumNumberOfAttempts, Configuration.Timeout));
                Configuration.Logger.LogWarning(string.Format(CultureInfo.InvariantCulture, Resources.Query, Token.SqlStatement));
            }
        }

        /// <summary>
        /// Print in the log the key not found.
        /// </summary>
        private void LogNoKeyFound()
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Warning))
            {
                Configuration.Logger.LogWarning(string.Format(CultureInfo.InvariantCulture, Resources.EntityKeyNotFound, entity, entity.GetType().Name));
            }
        }

        /// <summary>
        /// Print in the log before saving.
        /// </summary>
        private void LogSave()
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Information))
            {
                var state = GetEntityState();

                if (state == EntityState.Deleted)
                {
                    Configuration.Logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resources.DeletingEntity, GetTableName(), GetFriendlyName()));
                }
                else
                {
                    Configuration.Logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resources.SavingEntityInState, GetTableName(), GetFriendlyName(), state.ToString()));
                }
            }
        }

        /// <summary>
        /// Print in the log the save error.
        /// </summary>
        private void LogSaveError()
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Error))
            {
                Configuration.Logger.LogError(string.Format(CultureInfo.InvariantCulture, Resources.FailedToExecuteCommand, "Save", GetTableName(), GetFriendlyName()));
            }
        }

        /// <summary>
        /// Print in the log before loading.
        /// </summary>
        /// <param name="keyValue">The value of the key.</param>
        private void LogLoad(object keyValue)
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Information))
            {
                Configuration.Logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resources.LoadingEntity, GetTableName(), keyValue ?? GetFriendlyName()));
            }
        }

        /// <summary>
        /// Print in the log the loading error.
        /// </summary>
        private void LogLoadError()
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Error))
            {
                Configuration.Logger.LogError(string.Format(CultureInfo.InvariantCulture, Resources.FailedToExecuteCommand, "Load", GetTableName(), GetFriendlyName()));
            }
        }

        /// <summary>
        /// Print in the log the entity key error.
        /// </summary>
        /// <param name="keyValue">The value of the key.</param>
        private void LogEntityKeyNotFound(object keyValue)
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Warning))
            {
                Configuration.Logger.LogWarning(string.Format(CultureInfo.InvariantCulture, Resources.EntityKeyNotFound, GetTableName(), keyValue ?? provider.GetContextName()));
            }
        }

        /// <summary>
        /// Print in the log the list command.
        /// </summary>
        private void LogList()
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Information))
            {
                Configuration.Logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resources.ListEntity, GetTableName(), GetFriendlyName()));
            }
        }

        /// <summary>
        /// Print in the log the list command error.
        /// </summary>
        private void LogListError()
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Error))
            {
                Configuration.Logger.LogError(string.Format(CultureInfo.InvariantCulture, Resources.FailedToExecuteCommand, "List", GetTableName(), GetFriendlyName()));
            }
        }

        /// <summary>
        /// Print in the log the entity creation error.
        /// </summary>
        private void LogNewEntityError()
        {
            if (Configuration.CanUseLogging && Configuration.Logger.IsEnabled(LogLevel.Error))
            {
                Configuration.Logger.LogError(string.Format(CultureInfo.InvariantCulture, Resources.FailedToExecuteCommand, "NewEntity", GetTableName(), GetFriendlyName()));
            }
        }

        /// <summary>
        /// Change the entity to manage.
        /// </summary>
        /// <param name="entity">The instance of the entity to manage.</param>
        private void FireSetEntityObject(IEntityObjectIdentifier entity)
        {
            if (entity == null || !(entity is TEntity))
            {
                throw new EntityGateCoreException(Resources.UnableToHandleEntityType);
            }

            AppendEntity((TEntity)entity);
        }

        /// <summary>
        /// Marks the entity to the requested state.
        /// </summary>
        /// <param name="entity">The instance of the entity to manage.</param>
        /// <param name="targetState">The chosen state.</param>
        private IEntityObjectIdentifier MarkAs(IEntityObjectIdentifier entity, EntityState targetState)
        {
            try
            {
                entity = provider.GetManagedOrPocoEntity(entity, null);
                provider.ManageEntity(entity, null, targetState);
            }
            catch (Exception ex)
            {
                throw new EntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.FailedToExecuteCommand, $"Mark as {targetState}", entity, entity.GetContentInfo()), ex);
            }

            return entity;
        }

        /// <summary>
        /// Returns the instance of the entity.
        /// If no instance, create a blank instance (detached).
        /// </summary>
        /// <returns></returns>
        private TEntity GetOrCreateEntityObject()
        {
            if (!HasEntityObject)
            {
                ExecutionStart();
                MakeEntityObject();
            }

            return entity;
        }

        /// <summary>
        /// Execution of the creation of an entity.
        /// </summary>
        private void MakeEntityObject()
        {
            var newEntity = ReflectionHelper.MakeInstance<TEntity>(provider.GetPocoEntityType());
            AffectEntity(newEntity);
        }

        /// <summary>
        /// Returns the primary key to use.
        /// </summary>
        /// <param name="keyValue">The value of the key.</param>
        /// <returns></returns>
        private object GetEntityIdentifier(object keyValue)
        {
            if (keyValue == null)
            {
                if (!HasEntityObject)
                {
                    throw new EntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.EntityKeyNotFound, GetTableName(), provider.GetContextName()));
                }

                keyValue = entity.Identifier;
            }

            return keyValue;
        }

        /// <summary>
        /// Remove context caching.
        /// </summary>
        private void NoTracking()
        {
            Token.IsTracked = false;

            if (provider != null)
            {
                provider.NoTracking();
            }

            CleanOriginalValues();
        }

        /// <summary>
        /// Execute the loading of the entity.
        /// </summary>
        /// <param name="keyValue">The value of the key.</param>
        private bool LoadEntity(object keyValue)
        {
            var loaded = false;

            while (ExecutionAllowed())
            {
                PreparingExecution();
                var value = entity;

                try
                {
                    if (provider.TryGetEntity(keyValue, ref value))
                    {
                        AppendEntity(value);
                        loaded = true;
                        break;
                    }
                    else
                    {
                        LogEntityKeyNotFound(keyValue);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (!ExecutionAllowed(ex))
                    {
                        this.LogException(ex);
                        throw new CanceledEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToExecuteCommand, this.GetType().Name, Token.NumberOfAttempts), ex);
                    }
                }
            }

            return loaded;
        }

        /// <summary>
        /// Execution of the loading of EntitySet.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<TEntity> LoadEntitySet()
        {
            IEnumerable<TEntity> rslt = null;

            while (ExecutionAllowed())
            {
                PreparingExecution();

                try
                {
                    rslt = provider.MakeSet<TEntity>();
                    break;
                }
                catch (Exception ex)
                {
                    if (!ExecutionAllowed(ex))
                    {
                        this.LogException(ex);
                        throw new CanceledEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToExecuteCommand, this.GetType().Name, Token.NumberOfAttempts), ex);
                    }
                }
            }

            return rslt;
        }

        /// <summary>
        /// Execution of the save.
        /// </summary>
        private void SaveEntity()
        {
            while (ExecutionAllowed())
            {
                PreparingExecution();

                try
                {
                    AppendEntity(entity);

                    if (provider.HasChanges())
                    {
                        var oldState = GetEntityState();
                        var numberOfRowsChanged = provider.SaveChanges();
                        RefreshAfterSave(numberOfRowsChanged, oldState);
                    }
                    break;
                }
                catch (DbUpdateException ex)
                {
                    if (ex is DbUpdateConcurrencyException)
                    {
                        // In case of conflict, update the context before the next save.
                        provider.RefreshChanges(entity);
                    }

                    if (!ExecutionAllowed(ex))
                    {
                        this.LogException(ex);
                        throw new CanceledEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToExecuteCommand, this.GetType().Name, Token.NumberOfAttempts), ex);
                    }
                }
            }
        }

        /// <summary>
        /// Refreshing the information after a save of the context.
        /// </summary>
        /// <param name="numberOfRowsChanged">Number of lines changed.</param>
        /// <param name="oldState">The old state.</param>
        private void RefreshAfterSave(int numberOfRowsChanged, EntityState oldState)
        {
            switch (oldState)
            {
                case EntityState.Added:
                {
                    RefreshPrimaryKey();
                    break;
                }

                case EntityState.Deleted:
                {
                    if (numberOfRowsChanged < 1)
                    {
                        numberOfRowsChanged = 1;
                    }

                    break;
                }
            }

            Token.NumberOfRows = numberOfRowsChanged;
        }

        /// <summary>
        /// Returns the state of the entity.
        /// </summary>
        /// <returns></returns>
        private EntityState GetEntityState()
        {
            return HasEntityObject && provider != null ? provider.GetEntityState(entity) : EntityState.Unchanged;
        }

        /// <summary>
        /// Returns the name of the primary key.
        /// </summary>
        /// <returns></returns>
        private string GetPrimaryKeyFriendlyName()
        {
            var indexPair = GetPrimaryKey();
            var keyFriendlyName = IsNewEntity ? NewKeyValue : indexPair.Value;
            return string.Format(CultureInfo.InvariantCulture, "{0}={1}", indexPair.Key, keyFriendlyName);
        }

        /// <summary>
        /// Cleaning the origin values.
        /// </summary>
        private void CleanOriginalValues()
        {
            if (originalValues != null)
            {
                originalValues = null;
            }
        }

        /// <summary>
        /// Cleaning the keys.
        /// </summary>
        private void CleanPrimaryKeys()
        {
            if (primaryKeys != null)
            {
                primaryKeys.Clear();
                primaryKeys = null;
            }
        }

        /// <summary>
        /// Determines whether the entity type of the handler is usable.
        /// </summary>
        /// <returns></returns>
        private static bool HasEntityType()
        {
            return ReflectionHelper.IsRealType(typeof(TEntity));
        }

        /// <summary>
        /// Refreshes the values of the keys.
        ///
        /// The refresh is only for keys that are already in the cache.
        /// </summary>
        private void RefreshPrimaryKey()
        {
            if (HasEntityType())
            {
                foreach (var keyName in primaryKeys.Keys.ToArray())
                {
                    primaryKeys[keyName] = GetFieldValue(keyName);
                }
            }
            else
            {
                // The entity type is likely to have been changed, cleanup.
                CleanPrimaryKeys();
            }
        }

        /// <summary>
        /// Matches the requested entity (and its state) on the current context.
        /// </summary>
        /// <param name="nextEntity">The entity to analyze.</param>
        private void AppendEntity(TEntity nextEntity)
        {
            if (nextEntity == null)
            {
                throw new EntityGateCoreException(Resources.InvalidEntity);
            }

            if (nextEntity != entity)
            {
                AffectEntity(nextEntity);
            }

            // Consistency of the state of the entity.
            provider.ManageEntity(entity);
        }

        /// <summary>
        /// Assignment of the entity.
        /// </summary>
        /// <param name="externalEntity">The entity to manage.</param>
        private void AffectEntity(TEntity externalEntity)
        {
            // Refreshing the entity type.
            provider.SetPocoEntityType(externalEntity.GetType());
            entity = (TEntity)provider.GetManagedOrPocoEntity(externalEntity, provider.GetPocoEntityType());

            // Update the primary key.
            RefreshPrimaryKey();
        }

        /// <summary>
        /// Verification procedure for the automatic saving of the origin values.
        /// </summary>
        private void CheckAutoSaveOriginalValues()
        {
            if (Token.IsTracked)
            {
                CheckEntityForAutoSaveOriginalValues();
                FireAutoSaveOriginalValues();
            }
        }

        /// <summary>
        /// Automatic saving of the original values if necessary.
        /// </summary>
        private void FireAutoSaveOriginalValues()
        {
            if (Token.SaveOriginalValues && originalValues == null)
            {
                originalValues = GetOriginalValues();
            }
        }

        /// <summary>
        /// Automatically determines whether to save source values.
        /// </summary>
        private void CheckEntityForAutoSaveOriginalValues()
        {
            if (Configuration.AutomaticCheckOfOriginalValues)
            {
                var oldState = Token.SaveOriginalValues;
                var newState = entity.IsEntityArchival();
                Token.SaveOriginalValues = newState;

                if (oldState != newState && oldState)
                {
                    CleanOriginalValues();
                }
            }
        }
    }
}

