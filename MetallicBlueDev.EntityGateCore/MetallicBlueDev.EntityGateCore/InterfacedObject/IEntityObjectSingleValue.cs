namespace MetallicBlueDev.EntityGate.InterfacedObject
{
    /// <summary>
    /// Business object with a unique value.
    /// </summary>
    public interface IEntityObjectSingleValue : IEntityObjectIdentifier
    {
        /// <summary>
        /// Value of the object.
        /// </summary>
        /// <returns></returns>
        string SingleValue { get; set; }
    }
}

