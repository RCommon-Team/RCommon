using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    /// <summary>
    /// Configuration options applied during JSON deserialization.
    /// </summary>
    /// <seealso cref="IJsonSerializer"/>
    /// <seealso cref="JsonSerializeOptions"/>
    public class JsonDeserializeOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonDeserializeOptions"/> with default values.
        /// <see cref="CamelCase"/> defaults to <see langword="true"/>.
        /// </summary>
        public JsonDeserializeOptions()
        {
            this.CamelCase = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether property names should use camelCase naming policy
        /// during deserialization. Defaults to <see langword="true"/>.
        /// </summary>
        public bool CamelCase { get; set; }
    }
}
