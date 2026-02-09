using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    /// <summary>
    /// Configuration options applied during JSON serialization.
    /// </summary>
    /// <seealso cref="IJsonSerializer"/>
    /// <seealso cref="JsonDeserializeOptions"/>
    public class JsonSerializeOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonSerializeOptions"/> with default values.
        /// <see cref="CamelCase"/> defaults to <see langword="true"/> and <see cref="Indented"/> defaults to <see langword="false"/>.
        /// </summary>
        public JsonSerializeOptions()
        {
            this.CamelCase = true;
            this.Indented = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether property names should use camelCase naming policy
        /// during serialization. Defaults to <see langword="true"/>.
        /// </summary>
        public bool CamelCase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the serialized JSON output should be indented
        /// for readability. Defaults to <see langword="false"/>.
        /// </summary>
        public bool Indented { get; set; }
    }
}
