using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sc.Server.Weaver.Schema;

namespace Starcounter.Internal.Weaver {
    using DatabaseAttribute = Sc.Server.Weaver.Schema.DatabaseAttribute;

    /// <summary>
    /// Provides weaver helper methods for dynamic types.
    /// </summary>
    internal static class DynamicTypesHelper {
        /// <summary>
        /// Validates the given attribute to see if custom attributes are
        /// correctly used.
        /// </summary>
        /// <param name="attribute"></param>
        public static void ValidateDatabaseAttribute(DatabaseAttribute attribute) {
            var referencedType = attribute.AttributeType as DatabaseClass;
            if (referencedType == null) {
                // Only applies to databae classes
                // TODO:
            }

            if (attribute.IsTransient) {
                // Nope.
                // TODO:
            }

            // Check if it's incompatible with other type decorations, like
            // if its a TypeName or Inherits
            // TODO:
        }
    }
}