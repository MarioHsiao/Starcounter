using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represents the interface to a database type specification, as
    /// defined <a href="http://www.starcounter.com/internal/wiki/W3">
    /// here</a>.
    /// </summary>
    public sealed class TypeSpecification {
        /// <summary>
        /// Provides the type specification class name.
        /// </summary>
        public const string Name = "__starcounterTypeSpecification";

        internal TypeSpecification(Type typeSpecType) {

        }
    }
}
