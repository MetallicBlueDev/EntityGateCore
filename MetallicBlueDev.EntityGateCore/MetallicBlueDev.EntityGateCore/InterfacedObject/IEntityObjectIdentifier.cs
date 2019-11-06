namespace MetallicBlueDev.EntityGate.InterfacedObject
{
    /// <summary>
    /// Primary key of the business object.
    /// </summary>
    public interface IEntityObjectIdentifier
    {
        /// <summary>
        /// Returns the primary key of the business object.
        ///
        /// Get the key of the object. You should not have to know the exact type of key.
        /// </summary>
        /// <returns>The primary key can be of any type (INT, LONG, GUID, etc.).</returns>
        object Identifier { get; }
    }
}


