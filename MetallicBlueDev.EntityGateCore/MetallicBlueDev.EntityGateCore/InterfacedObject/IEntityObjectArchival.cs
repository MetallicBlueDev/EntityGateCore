using System;

namespace MetallicBlueDev.EntityGate.InterfacedObject
{
    /// <summary>
    /// Business object that can generate history events.
    /// </summary>
    public interface IEntityObjectArchival : IEntityObjectIdentifier
    {
        /// <summary>
        /// Returns the type of the entity that binds the history and the business object.
        /// </summary>
        /// <param name="subTypeName">The name of the sub entity related to the history (to be used as a key).</param>
        /// <returns></returns>
        Type GetHistoryLinkType(string subTypeName);
    }
}


