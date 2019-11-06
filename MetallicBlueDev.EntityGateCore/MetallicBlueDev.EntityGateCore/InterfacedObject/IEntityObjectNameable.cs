namespace MetallicBlueDev.EntityGate.InterfacedObject
{
    /// <summary>
    /// Business object with a name.
    /// </summary>
    public interface IEntityObjectNameable : IEntityObjectIdentifier
    {
        /// <summary>
        /// Current name of the object.
        ///
        /// The name or value of the object (for example "screwdriver" for a tool or "Width" for a parameter.
        /// </summary>
        /// <returns></returns>
        string Name { get; set; }
    }
}

