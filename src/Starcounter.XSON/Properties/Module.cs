

using Starcounter.Advanced.XSON;
using System.Collections.Generic;

namespace Modules {

    /// <summary>
    /// Represents this module
    /// </summary>
    internal static class Starcounter_XSON {

        /// <summary>
        /// Contains all dependency injections into this module
        /// </summary>
        internal static class Injections {

            /// <summary>
            /// Please inject the serializer factory here
            /// </summary>
            internal static ITypedJsonSerializerFactory TypedJsonSerializerFactory;

        }
    }
}