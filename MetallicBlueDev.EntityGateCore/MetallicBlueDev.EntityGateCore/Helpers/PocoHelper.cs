using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MetallicBlueDev.EntityGate.Extensions;
using MetallicBlueDev.EntityGate.InterfacedObject;

namespace MetallicBlueDev.EntityGate.Helpers
{
    /// <summary>
    /// Internal project support.
    /// </summary>
    internal static class PocoHelper
    {
        private static readonly string collectionRemoveMethodName = ReflectionHelper.GetPropertyName<HashSet<object>>(o => o.Remove(null));
        private static readonly string collectionAddMethodName = ReflectionHelper.GetPropertyName<HashSet<object>>(o => o.Add(null));
        private static readonly string collectionCountPropertyName = ReflectionHelper.GetPropertyName<HashSet<object>>(o => o.Count);

        /// <summary>
        /// Overwrites empty collections with a null value.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to check.</param>
        internal static void SetEmptyEntityCollectionAsNull<T>(T entity) where T : class
        {
            var properties = entity.GetType().GetProperties();

            foreach (var collectionField in ReflectionHelper.GetEntityCollectionProperties(properties))
            {
                var currentCollection = collectionField.GetValue(entity, null);

                if (currentCollection != null)
                {
                    var propertyInfo = collectionField.PropertyType.GetProperty(collectionCountPropertyName);
                    var value = propertyInfo.GetValue(currentCollection);

                    if (value is int && (int)value <= 0)
                    {
                        collectionField.SetValue(entity, null);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a pure instance of the entity, not the proxy version.
        /// If the entity is pure, the same instance is preserved.
        /// If the entity is proxy, a new instance is created with the same values.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to check.</param>
        /// <param name="contextEntityType">The type of reference entity. Do not put a type of proxy entity.</param>
        /// <param name="circularReferences">To solve circular dependency problems.</param>
        /// <param name="withDataRelation">With or without relationships (level 1 or higher).</param>
        /// <returns></returns>
        internal static T GetPocoEntity<T>(T entity, Type contextEntityType, ArrayList circularReferences = null, bool withDataRelation = false) where T : class
        {
            T result;
            var entityType = entity.GetType();

            // TODO
            //if (contextEntityType == null)
            //{
            //    contextEntityType = ObjectContext.GetObjectType(entityType);
            //}

            if (contextEntityType == entityType)
            {
                // If the entity is already a pure version, the clone is totally useless.
                result = entity;
            }
            else
            {
                result = ReflectionHelper.CloneEntity(entity, contextEntityType, withDataRelation);
            }

            if (circularReferences == null)
            {
                circularReferences = new ArrayList();
            }

            CheckPocoEntityInChildren(result, contextEntityType, circularReferences);

            return result;
        }

        /// <summary>
        /// Returns the value of the requested field.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="fieldName">Name of the column.</param>
        /// <returns></returns>
        internal static object GetFieldValue(IEntityObjectIdentifier entity, Type entityType, string fieldName)
        {
            object value = null;

            if (entity != null && fieldName.IsNotNullOrEmpty() && ReflectionHelper.IsRealType(entityType))
            {
                var fieldInfo = entityType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);

                if (fieldInfo != null)
                {
                    value = fieldInfo.GetValue(entity, null);
                }
            }

            return value;
        }

        /// <summary>
        /// Assigns a pure instance of entity at the child level of the current entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to check.</param>
        /// <param name="contextEntityType">The type of reference entity. Do not put a type of proxy entity.</param>
        /// <param name="circularReferences">To solve circular dependency problems.</param>
        private static void CheckPocoEntityInChildren<T>(T entity, Type contextEntityType, ArrayList circularReferences) where T : class
        {
            if (!circularReferences.Contains(entity))
            {
                circularReferences.Add(entity);

                var properties = contextEntityType.GetProperties();

                CheckPocoEntityInEntityFields(entity, properties, circularReferences);
                CheckPocoEntityInEntityCollections(entity, properties, circularReferences);
            }
        }

        /// <summary>
        /// Checks the assignment of a pure entity instance to fields containing an entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to check.</param>
        /// <param name="properties">Lists properties of the pure entity.</param>
        /// <param name="circularReferences">To solve circular dependency problems.</param>
        private static void CheckPocoEntityInEntityFields<T>(T entity, PropertyInfo[] properties, ArrayList circularReferences) where T : class
        {
            foreach (var entityField in ReflectionHelper.GetEntityClassProperties(properties))
            {
                var currentEntityObject = entityField.GetValue(entity, null);

                if (currentEntityObject != null)
                {
                    var newEntityObject = GetPocoEntity(currentEntityObject, null, circularReferences, withDataRelation: true);

                    if (currentEntityObject != newEntityObject)
                    {
                        entityField.SetValue(entity, newEntityObject, null);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the assignment of a pure instance of entity in the entity collections.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to check.</param>
        /// <param name="properties">Lists properties of the pure entity.</param>
        /// <param name="circularReferences">To solve circular dependency problems.</param>
        private static void CheckPocoEntityInEntityCollections<T>(T entity, PropertyInfo[] properties, ArrayList circularReferences) where T : class
        {
            foreach (var collectionField in ReflectionHelper.GetEntityCollectionProperties(properties))
            {
                var currentCollection = collectionField.GetValue(entity, null);

                if (currentCollection != null)
                {
                    ArrayList removeList = null;
                    ArrayList addList = null;

                    foreach (var currentEntityObject in (IEnumerable)currentCollection)
                    {
                        var newEntityObject = GetPocoEntity(currentEntityObject, null, circularReferences, withDataRelation: true);

                        if (currentEntityObject != newEntityObject)
                        {
                            if (removeList == null)
                            {
                                removeList = new ArrayList();
                            }

                            if (addList == null)
                            {
                                addList = new ArrayList();
                            }

                            removeList.Add(currentEntityObject);
                            addList.Add(newEntityObject);
                        }
                    }

                    if (removeList != null)
                    {
                        var removeMethod = collectionField.PropertyType.GetMethod(collectionRemoveMethodName);
                        var addMethod = collectionField.PropertyType.GetMethod(collectionAddMethodName);

                        for (int i = 0, loopTo = removeList.Count - 1; i <= loopTo; i++)
                        {
                            removeMethod.Invoke(currentCollection, new object[] { removeList[i] });
                            addMethod.Invoke(currentCollection, new object[] { addList[i] });
                        }
                    }
                }
            }
        }
    }
}

