using System;
using System.Collections.Generic;
using MetallicBlueDev.EntityGate.InterfacedObject;
using Microsoft.EntityFrameworkCore;

namespace MetallicBlueDev.EntityGate.Helpers
{
    /// <summary>
    /// Help with context manipulation.
    /// </summary>
    internal class ContextHelper
    {
        /// <summary>
        /// Creating an DbSet.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static IEnumerable<IEntityObjectIdentifier> Set(DbContext context, Type t)
        {
            return (IEnumerable<IEntityObjectIdentifier>)context.GetType().GetMethod("Set").MakeGenericMethod(t).Invoke(context, null);
        }

        /// <summary>
        /// Determines whether the entity / type combination appears valid.
        /// </summary>
        /// <typeparam name="TContext">The type of context.</typeparam>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="contextType">The type of the context.</param>
        /// <returns></returns>
        internal static bool IsValidContext<TContext>(Type entityType, Type contextType) where TContext : DbContext
        {
            return typeof(TContext).IsAssignableFrom(contextType)
                && entityType.Namespace.Equals(contextType.Namespace, StringComparison.InvariantCulture);
        }
    }
}

