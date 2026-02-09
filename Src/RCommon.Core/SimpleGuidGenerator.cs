using System;

namespace RCommon
{
    /// <summary>
    /// Implements <see cref="IGuidGenerator"/> by using <see cref="Guid.NewGuid"/>.
    /// </summary>
    public class SimpleGuidGenerator : IGuidGenerator
    {
        

        /// <summary>
        /// Creates a new random <see cref="Guid"/> using <see cref="Guid.NewGuid"/>.
        /// </summary>
        /// <returns>A new randomly generated <see cref="Guid"/>.</returns>
        public virtual Guid Create()
        {
            return Guid.NewGuid();
        }
    }
}
