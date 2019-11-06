using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using MetallicBlueDev.EntityGate.Extensions;
using MetallicBlueDev.EntityGate.GateException;
using MetallicBlueDev.EntityGateCore.Properties;

namespace MetallicBlueDev.EntityGate.Helpers
{
    /// <summary>
    /// Help with the manipulation of object reflection.
    /// </summary>
    internal class ReflectionHelper
    {
        /// <summary>
        /// Create the following type the full name of the type.
        /// </summary>
        /// <param name="pTypeFullName">Full name of the type.</param>
        /// <returns></returns>
        public static Type MakeType(string pTypeFullName)
        {
            Type rslt = null;

            if (pTypeFullName.IsNotNullOrEmpty())
            {
                try
                {
                    rslt = Type.GetType(pTypeFullName, true, true);
                }
                catch (Exception ex)
                {
                    throw new ReflectionEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToCreateType, pTypeFullName), ex);
                }
            }

            return rslt;
        }

        /// <summary>
        /// Creation of an object (local reference).
        /// The object must be loaded into memory.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="args">Additional arguments.</param>
        /// <returns></returns>
        internal static TObject MakeInstance<TObject>(Type type, params object[] args)
        {
            var rslt = default(TObject);

            if (type != null)
            {
                object rsltObject;

                try
                {
                    rsltObject = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);

                    if (rsltObject != null)
                    {
                        // Force typecasting, do not use TypeOf here
                        rslt = (TObject)rsltObject;
                    }
                }
                catch (Exception ex)
                {
                    throw new ReflectionEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToCreateObject, typeof(TObject).Name, type?.Name), ex);
                }
            }

            if (rslt == null)
            {
                throw new ReflectionEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToCreateObject, typeof(TObject).Name, type?.Name));
            }

            return rslt;
        }

        /// <summary>
        /// Returns the name of the property or method.
        /// Does not require any object instance.
        /// </summary>
        /// <typeparam name="TObject">The type of object.</typeparam>
        /// <param name="exp">The property or method in question.</param>
        /// <returns></returns>
        internal static string GetPropertyName<TObject>(Expression<Func<TObject, object>> exp)
        {
            string propertyName = null;

            if (exp.Body.NodeType == ExpressionType.MemberAccess)
            {
                propertyName = ((MemberExpression)exp.Body).Member.Name;
            }
            else if (exp.Body.NodeType == ExpressionType.Call)
            {
                propertyName = ((MethodCallExpression)exp.Body).Method.Name;
            }
            else if (exp.Body.NodeType == ExpressionType.Convert)
            {
                var expression = ((UnaryExpression)exp.Body).Operand;

                if (expression is MemberExpression)
                {
                    propertyName = ((MemberExpression)expression).Member.Name;
                }
                else if (expression is MethodCallExpression)
                {
                    propertyName = ((MethodCallExpression)expression).Method.Name;
                }
            }

            if (propertyName == null)
            {
                throw new ReflectionEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToGetPropertyName, exp.ToString()));
            }

            return propertyName;
        }

        /// <summary>
        /// Returns a true copy of the entity.
        /// </summary>
        /// <typeparam name="TObject">The type of object.</typeparam>
        /// <param name="source">The entity to clone.</param>
        /// <param name="entityType">The type of reference entity. Do not put a type of proxy entity.</param>
        /// <param name="withDataRelation">With or without relationships (level 1 or higher).</param>
        /// <returns></returns>
        internal static TObject CloneEntity<TObject>(TObject source, Type entityType, bool withDataRelation) where TObject : class
        {
            TObject result = null;

            if (source != null && entityType != null)
            {
                result = MakeInstance<TObject>(entityType);

                foreach (var info in GetReadWriteProperties(entityType, withDataRelation))
                {
                    var valueObject = info.GetValue(source, null);
                    var currentValueObject = info.GetValue(result, null);

                    if (valueObject != null && !valueObject.Equals(currentValueObject))
                    {
                        info.SetValue(result, valueObject, null);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the basic properties of the entity that are read and write accessible.
        /// Represents the columns in database.
        /// </summary>
        /// <param name="entityType">The reference entity. Do not put a proxy entity.</param>
        /// <param name="withDataRelation">With or without relationships (level 1 or higher).</param>
        internal static IEnumerable<PropertyInfo> GetReadWriteProperties(Type entityType, bool withDataRelation)
        {
            return entityType.GetProperties()
                .Where(info => info.CanRead
                && info.CanWrite
                && (!info.PropertyType.IsClass || info.PropertyType == typeof(string))
                && (withDataRelation || !typeof(IEnumerable).IsAssignableFrom(info.PropertyType)));
        }

        /// <summary>
        /// Returns the properties containing an entity.
        /// </summary>
        /// <param name="properties">Lists properties of the pure entity.</param>
        /// <returns></returns>
        internal static IEnumerable<PropertyInfo> GetEntityClassProperties(PropertyInfo[] properties)
        {
            return properties
                .Where(info => info.CanRead
                && info.CanWrite
                && info.PropertyType.IsClass && info.PropertyType != typeof(string));
        }

        /// <summary>
        /// Returns the properties containing an entity.
        /// </summary>
        /// <param name="properties">Lists properties of the pure entity.</param>
        /// <returns></returns>
        internal static IEnumerable<PropertyInfo> GetEntityCollectionProperties(PropertyInfo[] properties)
        {
            return properties
                .Where(info => info.CanRead
                && info.CanWrite
                && !info.PropertyType.IsClass
                && typeof(IEnumerable).IsAssignableFrom(info.PropertyType));
        }

        /// <summary>
        /// Construct entity information as a string.
        /// </summary>
        /// <param name="source">The source entity.</param>
        /// <param name="content">String builder.</param>
        internal static void BuiltContentInfo(object source, StringBuilder content)
        {
            if (source != null)
            {
                var sourceType = source.GetType();

                try
                {
                    foreach (var info in GetReadWriteProperties(sourceType, false))
                    {
                        var valueObject = info.GetValue(source, null);

                        if (valueObject == null)
                        {
                            continue;
                        }

                        content.Append(info.Name);
                        content.Append("=");
                        content.Append(valueObject);
                        content.Append(", ");
                    }
                }
                catch (Exception ex)
                {
                    throw new ReflectionEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToObtainInformationContainedInEntity, sourceType.Name), ex);
                }
            }
        }

        /// <summary>
        /// Returns the corresponding SQL Server error.
        /// If no SQL error found, return <code>null</code>.
        /// </summary>
        /// <param name="ex">Internal error</param>
        /// <returns></returns>
        internal static SqlException GetSqlServerException(Exception ex)
        {
            SqlException sqlEx = null;

            if (ex != null)
            {
                if (!(ex is SqlException))
                {
                    // Search by going back the exceptions.
                    while (ex.InnerException != null)
                    {
                        if (ex is SqlException)
                        {
                            break;
                        }

                        ex = ex.InnerException;
                    }
                }

                if (ex is SqlException)
                {
                    sqlEx = (SqlException)ex;
                }
            }

            return sqlEx;
        }

        /// <summary>
        /// Determine if the type is not abstract.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns></returns>
        internal static bool IsRealType(Type entityType)
        {
            return entityType != null
                && !entityType.IsInterface
                && !entityType.IsAbstract;
        }
    }
}

