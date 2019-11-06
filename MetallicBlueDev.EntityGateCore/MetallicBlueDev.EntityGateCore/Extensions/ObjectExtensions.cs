using MetallicBlueDev.EntityGate.InterfacedObject;

namespace MetallicBlueDev.EntityGate.Extensions
{
    /// <summary>
    /// Help with the manipulation of <see cref="IEntityObjectIdentifier"/>.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Determines whether the object is an <see cref="IEntityObjectIdentifier"/>.
        /// </summary>
        /// <param name="o">The object to check.</param>
        /// <returns></returns>
        public static bool IsEntityObject(this object o)
        {
            return o is IEntityObjectIdentifier;
        }
    }
}

