using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MetallicBlueDev.EntityGate.Extensions;
using MetallicBlueDev.EntityGate.GateException;
using MetallicBlueDev.EntityGate.Helpers;
using MetallicBlueDev.EntityGate.InterfacedObject;
using MetallicBlueDev.EntityGateCore.Core;
using MetallicBlueDev.EntityGateCore.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace MetallicBlueDev.EntityGate.Core
{
    /// <summary>
    /// Dynamic Proxy Entities (POCO Proxy).
    /// 
    /// Entity framework service provider.
    /// Creation of the context related to the database.
    /// </summary>
    /// <typeparam name="TContext">The type of context.</typeparam>
    [Serializable()]
    internal sealed class EntityGateProvider<TContext> : IDisposable where TContext : DbContext
    {
        private readonly IEntityGateObject gate;
        private readonly EntityGateTracking tracking;

        private bool disposed = false;
        private Type pocoEntityType = null;
        private bool lazyLoading = false;

        [NonSerialized()]
        private TContext context = null;

        internal EntityGateProvider(IEntityGateObject gate)
        {
            this.gate = gate;
            tracking = new EntityGateTracking();
            lazyLoading = gate.Configuration.LazyLoading;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                FreeMemory();
                disposed = true;
            }
        }

        /// <summary>
        /// Returns the created context.
        /// </summary>
        /// <returns></returns>
        internal TContext GetContext()
        {
            return context;
        }

        /// <summary>
        /// Additional initialization of the service provider.
        /// </summary>
        internal void Initialize()
        {
            if (disposed)
            {
                throw new CanceledEntityGateCoreException(Resources.ObjectDisposed);
            }

            if (context == null || gate.Configuration.Changed)
            {
                // Creation of a new context.
                context = MakeContext();
                InitializeContext();
            }

            if (!gate.Token.IsTracked)
            {
                CleanTracking();
            }
        }

        /// <summary>
        /// Cleaning the tracking.
        /// </summary>
        internal void CleanTracking()
        {
            tracking.CleanTracking();
        }

        /// <summary>
        /// Stop monitoring.
        /// </summary>
        internal void NoTracking()
        {
            CleanTracking();
            gate.Token.IsTracked = false;
        }

        /// <summary>
        /// Returns the state of the entity.
        /// </summary>
        /// <param name="entity">The instance of the entity.</param>
        /// <returns></returns>
        internal EntityState GetEntityState(IEntityObjectIdentifier entity)
        {
            return GetEntityEntry(entity).State;
        }

        /// <summary>
        /// Change the type of entity currently managed.
        /// </summary>
        /// <param name="entityType">The type of the entity (pure or proxy).</param>
        internal void SetPocoEntityType(Type entityType)
        {
            if (pocoEntityType != entityType)
            {
                if (!ReflectionHelper.IsRealType(entityType))
                {
                    throw new ProviderEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidEntityType, entityType?.Name));
                }

                // Pure entity type.
                pocoEntityType = GetEntityTypeInfo(entityType).ClrType;

                if (gate.Configuration.CanUseLogging && gate.Configuration.Logger.IsEnabled(LogLevel.Information))
                {
                    gate.Configuration.Logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resources.CurrentEntityTypeState, pocoEntityType));
                }

                if (!pocoEntityType.IsSerializable)
                {
                    throw new ProviderEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.EntityTypeIsNotSerializable, pocoEntityType.Name));
                }
            }
        }

        /// <summary>
        /// Returns the currently managed entity type (pure / not proxy).
        /// </summary>
        /// <returns></returns>
        internal Type GetPocoEntityType()
        {
            if (pocoEntityType == null)
            {
                // Security on an incoherent call.
                throw new ProviderEntityGateCoreException(Resources.EntityTypeUndefined);
            }

            return pocoEntityType;
        }

        /// <summary>
        /// Management of POCO entity monitoring.
        /// </summary>
        internal void ManagePocoEntitiesTracking()
        {
            var isProxyCreationEnabled = context.ChangeTracker.LazyLoadingEnabled;
            context.ChangeTracker.LazyLoadingEnabled = false;

            try
            {
                FireManagePocoEntitiesTracking();
            }
            catch (Exception ex)
            {
                throw new ProviderEntityGateCoreException(Resources.FailedToTrackPocoEntities, ex);
            }
            finally
            {
                context.ChangeTracker.LazyLoadingEnabled = isProxyCreationEnabled;
            }
        }

        /// <summary>
        /// Returns the main entity since tracking.
        /// </summary>
        /// <returns></returns>
        internal TEntity GetMainEntity<TEntity>() where TEntity : class, IEntityObjectIdentifier
        {
            var mainEntity = tracking.GetMainEntity();
            return (TEntity)mainEntity;
        }

        /// <summary>
        /// Determines whether a specific entity type is currently being managed.
        /// </summary>
        /// <returns></returns>
        internal bool HasPocoEntityType()
        {
            return pocoEntityType != null;
        }

        /// <summary>
        /// Returns the names of the primary key columns.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<string> GetPrimaryKeys(IEntityObjectIdentifier entity)
        {
            var entityType = HasPocoEntityType() ? GetPocoEntityType() : entity.GetType();
            var key = GetEntityTypeInfo(entityType).FindPrimaryKey();

            if (key == null)
            {
                throw new ProviderEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.EntityKeyNotFound, entity, entityType.Name));
            }

            return key.Properties.Select(prop => prop.Name);
        }

        /// <summary>
        /// Returns the name of the current context.
        /// </summary>
        /// <returns></returns>
        internal string GetContextName()
        {
            return context.GetType().Name;
        }

        /// <summary>
        /// Change the deferred loading option.
        /// </summary>
        /// <param name="enabled">State of LazyLoading.</param>
        internal void ChangeLazyLoading(bool enabled)
        {
            lazyLoading = enabled;

            if (gate.Configuration.CanUseLogging && gate.Configuration.Logger.IsEnabled(LogLevel.Information))
            {
                gate.Configuration.Logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resources.LazyLoadingState, enabled));
            }

            context.ChangeTracker.LazyLoadingEnabled = enabled;
        }

        /// <summary>
        /// Creating an DbSet.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <returns></returns>
        internal IEnumerable<TEntity> MakeSet<TEntity>() where TEntity : class, IEntityObjectIdentifier
        {
            IEnumerable<TEntity> currentDbSet;

            if (ReflectionHelper.IsRealType(typeof(TEntity)))
            {
                currentDbSet = context.Set<TEntity>();
            }
            else
            {
                currentDbSet = ContextHelper.Set(context, GetPocoEntityType()).Cast<TEntity>();
            }

            return currentDbSet;
        }

        /// <summary>
        /// Determines whether the entity is known to the context.
        /// </summary>
        /// <param name="entity">The instance of the entity.</param>
        /// <returns></returns>
        internal bool HasEntity(IEntityObjectIdentifier entity)
        {
            IEntityObjectIdentifier test = null;
            return TryGetEntity(entity.Identifier, ref test) && test.Identifier.Equals(entity.Identifier);
        }

        /// <summary>
        /// Determines whether changes have been made to the context.
        /// </summary>
        /// <returns></returns>
        internal bool HasChanges()
        {
            return context.ChangeTracker.HasChanges();
        }

        /// <summary>
        /// Returns the modified and followed entities.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<EntityStateTracking> GetChangedEntries()
        {
            return tracking.GetEntities()
                .Where(entity => entity.State != EntityState.Unchanged && entity.State != EntityState.Detached);
        }

        /// <summary>
        /// Attempt to obtain the entity via the context.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="keyValue">The key to load.</param>
        /// <param name="entity">The instance of the entity.</param>
        /// <returns></returns>
        internal bool TryGetEntity<TEntity>(object keyValue, ref TEntity entity) where TEntity : class, IEntityObjectIdentifier
        {
            entity = ReflectionHelper.IsRealType(typeof(TEntity))
                ? context.Set<TEntity>().Find(keyValue)
                : (TEntity)context.Find(GetPocoEntityType(), keyValue);

            return entity != null;
        }

        /// <summary>
        /// Returns an entity instance manageable by the current context.
        /// </summary>
        /// <param name="externalEntity">The entity to check.</param>
        /// <param name="contextEntityType">The type of the entity or null.</param>
        /// <returns></returns>
        internal IEntityObjectIdentifier GetManagedOrPocoEntity(IEntityObjectIdentifier externalEntity, Type contextEntityType)
        {
            if (!HasEntity(externalEntity))
            {
                // Search in instances already followed.
                var entityTracked = GetEntityTracked(externalEntity, true);

                if (entityTracked != null)
                {
                    // This is an optimization, instead of cloning stupidly, we reuse the known instance with a merge managed by EF.
                    externalEntity = entityTracked;
                }
                else
                {
                    // Creation of a pure entity if necessary.
                    externalEntity = PocoHelper.GetPocoEntity(externalEntity, contextEntityType, withDataRelation: true);
                }
            }

            return externalEntity;
        }

        /// <summary>
        /// Applies the changes of the entity to the context.
        /// </summary>
        /// <param name="entity">The entity to manage.</param>
        internal void ManageEntity(IEntityObjectIdentifier entity)
        {
            var currentState = GetEntityState(entity);
            var targetState = GetEntityStateTargeted(entity, currentState);
            ManageEntity(entity, currentState, targetState);
        }

        /// <summary>
        /// Applies the changes of the entity to the context.
        /// </summary>
        /// <param name="entity">The entity to manage.</param>
        /// <param name="currentState">The original state (optional).</param>
        /// <param name="targetState">The chosen state.</param>
        internal void ManageEntity(IEntityObjectIdentifier entity, EntityState? currentState, EntityState targetState)
        {
            if (gate.Configuration.CanUseLogging && gate.Configuration.Logger.IsEnabled(LogLevel.Information))
            {
                gate.Configuration.Logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resources.ChangeEntityToState, entity, targetState));
            }

            if (targetState == EntityState.Detached)
            {
                throw new ProviderEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnexpectedEntityState, targetState, entity));
            }

            // The current state is not needed for an addition.
            if (!currentState.HasValue && targetState != EntityState.Added)
            {
                currentState = GetEntityState(entity);
            }

            // If the entity is detached, check the consistency.
            if (currentState.HasValue && currentState == EntityState.Detached)
            {
                var entityTracked = GetEntityTracked(entity, targetState != EntityState.Deleted);
                entity = entityTracked ?? entity;
            }

            GetEntityEntry(entity).State = targetState;
        }

        /// <summary>
        /// Saves the changes made to the context.
        /// </summary>
        /// <returns></returns>
        internal int SaveChanges()
        {
            DetectChanges();
            DetectLocalMode();

            if (!tracking.HasEntities())
            {
                throw new ProviderEntityGateCoreException(Resources.NoTrackedEntity);
            }

            if (gate.Configuration.CanUseLogging && gate.Configuration.Logger.IsEnabled(LogLevel.Information))
            {
                gate.Configuration.Logger.LogInformation(Resources.SavingChanges);
            }

            return context.SaveChanges();
        }

        /// <summary>
        /// Refreshes the entity via the context.
        /// </summary>
        /// <param name="entity"></param>
        internal void RefreshChanges(IEntityObjectIdentifier entity)
        {
            if (entity == null)
            {
                throw new ProviderEntityGateCoreException(Resources.InvalidEntityRefreshChanges);
            }

            GetEntityEntry(entity).Reload();
        }

        /// <summary>
        /// Returns the record of the original values of the entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="allProperties">Return all properties or just the modified ones.</param>
        /// <returns></returns>
        internal KeyValuePair<string, object>[] GetOriginalValues(IEntityObjectIdentifier entity, bool allProperties)
        {
            var values = new List<KeyValuePair<string, object>>();
            var entityEntry = GetEntityEntry(entity);
            var modifiedProperties = !allProperties ? entityEntry.Properties.Where(prop => prop.IsModified).Select(prop => prop.Metadata.Name).ToArray() : null;
            var originalValues = entityEntry.OriginalValues;

            foreach (var originalValue in originalValues.Properties)
            {
                var fieldName = originalValue.Name;

                if (modifiedProperties != null && !modifiedProperties.Any(name => name.EqualsIgnoreCase(fieldName)))
                {
                    continue;
                }

                values.Add(new KeyValuePair<string, object>(originalValue.Name, originalValues.GetValue<object>(originalValue.Name)));
            }

            return values.ToArray();
        }

        /// <summary>
        /// Returns information about the type of entity.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        private IEntityType GetEntityTypeInfo(Type entityType)
        {
            var entityTypeInfo = context.Model.FindRuntimeEntityType(entityType);
            //var entityTypeInfo = context.Model.GetEntityTypes(entityType).FirstOrDefault();

            if (entityTypeInfo == null || entityTypeInfo.ClrType == null)
            {
                throw new ProviderEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidEntityType, pocoEntityType.Name));
            }

            return entityTypeInfo;
        }

        /// <summary>
        /// Procedure for cleaning up used resources.
        /// </summary>
        private void FreeMemory()
        {
            CleanTracking();

            if (context != null)
            {
                context.Dispose();
                context = null;
            }

            pocoEntityType = null;
        }

        /// <summary>
        /// Check if the connection configuration needs to be synchronized.
        /// </summary>
        private void CheckConnectionConfiguration()
        {
            if (gate.Configuration.Changed)
            {
                // TODO
                //gate.Configuration.Update(context.Database.Connection);
            }
        }

        /// <summary>
        /// Returns the state to apply to the entity.
        /// </summary>
        /// <param name="entity">The entity to manage.</param>
        /// <param name="currentState">The original state.</param>
        /// <returns></returns>
        private EntityState GetEntityStateTargeted(IEntityObjectIdentifier entity, EntityState currentState)
        {
            var targetState = currentState;

            // Request the addition if necessary.
            if (targetState != EntityState.Added || !HasEntity(entity))
            {
                // Protection against inconsistent states.
                if (!entity.HasValidEntityKey())
                {
                    targetState = EntityState.Added;
                }
            }

            // Ask for attachment if necessary.
            if (targetState == EntityState.Detached)
            {
                targetState = EntityState.Modified;
            }

            return targetState;
        }

        /// <summary>
        /// POCO entity tracking audit routine.
        /// </summary>
        private void FireManagePocoEntitiesTracking()
        {
            if (gate.Token.IsTracked)
            {
                // Follow-up on all entities.
                TrackPocoEntities();
            }
            else
            {
                // Follow-up only of the main entity.
                TrackMainPocoEntity();
            }
        }

        /// <summary>
        /// Request tracking of all (modified) entities.
        /// </summary>
        private void TrackPocoEntities()
        {
            var mainEntity = gate.CurrentEntityObject;

            foreach (var entry in GetEntriesTracked().Where(entity => entity.State != EntityState.Unchanged || entity.Entity == mainEntity))
            {
                TrackPocoEntity(entry, entry.Entity == mainEntity);
            }
        }

        /// <summary>
        /// Request tracking of the main entity.
        /// </summary>
        private void TrackMainPocoEntity()
        {
            var entry = GetEntityEntry(gate.CurrentEntityObject);
            TrackPocoEntity(entry, true);
        }

        /// <summary>
        /// Request tracking of an entity.
        /// </summary>
        /// <param name="entry">Tracking information about the entity.</param>
        /// <param name="isMainEntry">Determine if this is the main entity.</param>
        private void TrackPocoEntity(EntityEntry entry, bool isMainEntry)
        {
            var pocoEntity = entry.State == EntityState.Deleted ? entry.OriginalValues.ToObject() : entry.CurrentValues.ToObject();
            tracking.MarkEntity(pocoEntity, entry.State, isMainEntry);
        }

        /// <summary>
        /// Returns the entities followed in the context.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<EntityEntry> GetEntriesTracked()
        {
            return context.ChangeTracker.Entries();
        }

        /// <summary>
        /// Force detection of changes (only if necessary, default is not the case).
        /// </summary>
        private void DetectChanges()
        {
            if (!context.ChangeTracker.AutoDetectChangesEnabled)
            {
                if (gate.Configuration.CanUseLogging && gate.Configuration.Logger.IsEnabled(LogLevel.Information))
                {
                    gate.Configuration.Logger.LogInformation(Resources.DetectionOfChanges);
                }

                context.ChangeTracker.DetectChanges();
            }
        }

        /// <summary>
        /// Detects local mode (use without serialization).
        /// </summary>
        private void DetectLocalMode()
        {
            if (!tracking.HasEntities())
            {
                // Starts tracking management.
                ManagePocoEntitiesTracking();
            }
        }

        /// <summary>
        /// Returns the instance of the entity since the trace, with update if requested.
        /// </summary>
        /// <param name="entity">The entity to manage.</param>
        /// <param name="updateValues">Update the tracked entity with the values of the entity to be managed.</param>
        /// <returns></returns>
        private IEntityObjectIdentifier GetEntityTracked(IEntityObjectIdentifier entity, bool updateValues)
        {
            IEntityObjectIdentifier trackedEntity = null;
            var currentEntry = GetEntityEntryTracked(entity);

            if (currentEntry != null)
            {
                if (updateValues)
                {
                    currentEntry.CurrentValues.SetValues(entity);
                }

                trackedEntity = (IEntityObjectIdentifier)currentEntry.Entity;
            }

            return trackedEntity;
        }

        /// <summary>
        /// Returns the entity information from the trace.
        /// </summary>
        /// <param name="entity">The entity to manage.</param>
        /// <returns></returns>
        private EntityEntry GetEntityEntryTracked(IEntityObjectIdentifier entity)
        {
            var sourceType = entity.GetType();
            return GetEntriesTracked()
                .FirstOrDefault(entry => entry.Entity.GetType() == sourceType
                && ((IEntityObjectIdentifier)entry.Entity).Identifier.Equals(entity.Identifier));
        }

        /// <summary>
        /// Returns the tracking information about the entity.
        /// </summary>
        /// <param name="entity">The entity to manage.</param>
        /// <returns></returns>
        private EntityEntry GetEntityEntry(object entity)
        {
            if (entity == null)
            {
                throw new ProviderEntityGateCoreException(Resources.InvalidEntity);
            }

            var entityEntry = context.Entry(entity);

            if (entityEntry == null)
            {
                throw new ProviderEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidEntityEntryFor, entity));
            }

            return entityEntry;
        }

        /// <summary>
        /// Initialization of the context.
        /// </summary>
        private void InitializeContext()
        {
            if (context == null)
            {
                throw new ProviderEntityGateCoreException(Resources.InvalidContext);
            }

            if (gate.Configuration.CanUseLogging && gate.Configuration.Logger.IsEnabled(LogLevel.Information))
            {
                gate.Configuration.Logger.LogInformation(Resources.InitializingContext);
            }

            InitializeConfiguration();
            ApplyTrackingIfNeeded();
        }

        /// <summary>
        /// Applies entity tracking on the context as needed.
        /// </summary>
        private void ApplyTrackingIfNeeded()
        {
            if (tracking.HasEntities() && gate.Token.IsTracked)
            {
                if (gate.Configuration.CanUseLogging && gate.Configuration.Logger.IsEnabled(LogLevel.Information))
                {
                    gate.Configuration.Logger.LogInformation(Resources.ApplyEntityTracking);
                }

                try
                {
                    gate.Token.IsTracked = false;
                    tracking.UnloadEmptyEntityCollection();
                    ApplyTracking();
                }
                finally
                {
                    gate.Token.IsTracked = true;
                }
            }
        }

        /// <summary>
        /// Applies entity tracking on the context.
        /// </summary>
        private void ApplyTracking()
        {
            foreach (var stateTracking in tracking.GetEntities())
            {
                ManageEntity(stateTracking.EntityObject, null, stateTracking.State);
            }
        }

        /// <summary>
        /// Initialization of the context configuration.
        /// </summary>
        private void InitializeConfiguration()
        {
            ChangeLazyLoading(lazyLoading);
            CheckConnectionConfiguration();
        }

        /// <summary>
        /// Returns a new context.
        /// </summary>
        /// <returns></returns>
        private TContext MakeContext()
        {
            TContext rslt;

            if (gate.Configuration.CanUseLogging && gate.Configuration.Logger.IsEnabled(LogLevel.Information))
            {
                gate.Configuration.Logger.LogInformation(Resources.MakingNewContext);
            }

            rslt = HasPocoEntityType() ? NewContextByEntityType() : NewContextByInstance(typeof(TContext));

            return rslt;
        }

        /// <summary>
        /// Returns the configured instance of the context according to the entity type.
        /// </summary>
        /// <returns></returns>
        private TContext NewContextByEntityType()
        {
            TContext context = null;
            var currentEntityType = GetPocoEntityType();

            // Search for a context that seems usable with the entity.
            foreach (var newContextType in currentEntityType.Assembly.GetTypes().Where(contextType => ContextHelper.IsValidContext<TContext>(currentEntityType, contextType)))
            {
                // Attempt to create the context.
                context = NewContextByInstance(newContextType);

                if (context.Model.FindEntityType(currentEntityType) != null)
                {
                    break;
                }
            }

            if (context == null)
            {
                throw new ProviderEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.FailedToCreateContextWithEntityType, currentEntityType.Name));
            }

            return context;
        }

        /// <summary>
        /// Returns the configured instance of the context according to the entity type and the connection string.
        /// </summary>
        /// <param name="contextType">The type of the context.</param>
        /// <returns></returns>
        private TContext NewContextByInstance(Type contextType)
        {
            TContext context = null;

            foreach (var currentConstructorInfo in contextType.GetConstructors())
            {
                context = NewContextByInstance(currentConstructorInfo);

                if (context != null)
                {
                    break;
                }
            }

            if (context == null)
            {
                throw new ProviderEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.FailedToCreateContextWithType, contextType.Name));
            }

            return context;
        }

        /// <summary>
        /// Returns the configured instance of the context according to the entity type and the connection string.
        /// </summary>
        /// <param name="contextType">The type of the context.</param>
        /// <param name="constructorInfo">The constructor of the type.</param>
        /// <returns></returns>
        private TContext NewContextByInstance(ConstructorInfo constructorInfo)
        {
            TContext context = null;
            var parameterInfos = constructorInfo.GetParameters();

            if (parameterInfos.Length == 1)
            {
                var currentParameterInfo = (ParameterInfo)parameterInfos.GetValue(0);

                if (currentParameterInfo.ParameterType == typeof(DbContextOptions))
                {
                    context = (TContext)constructorInfo.Invoke(new object[] { CreateDbContextOptions() });
                }
            }

            return context;
        }

        /// <summary>
        /// The options to be used by a DbContext.
        /// </summary>
        /// <returns></returns>
        private DbContextOptions CreateDbContextOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            gate.Configuration.Update(optionsBuilder);
            optionsBuilder.AddInterceptors(new HintCommandInterceptor(gate));
            return optionsBuilder.Options;
        }
    }
}

