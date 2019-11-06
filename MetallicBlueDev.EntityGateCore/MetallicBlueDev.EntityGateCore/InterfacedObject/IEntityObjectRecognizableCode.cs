namespace MetallicBlueDev.EntityGate.InterfacedObject
{
    /// <summary>
    /// Business object with an identification code.
    /// </summary>
    public interface IEntityObjectRecognizableCode : IEntityObjectIdentifier
    {
        /// <summary>
        /// Recognition code (theoretically unique).
        ///
        /// Calls the object via a unique code (in the processing of your application for example).
        ///
        /// For example "My_Code".
        /// </summary>
        /// <returns></returns>
        string CodeName { get; set; }
    }
}

