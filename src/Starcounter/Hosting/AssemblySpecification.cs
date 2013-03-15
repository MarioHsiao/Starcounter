
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represents the interface to a assembly specification, as it is
    /// defined <a href="http://www.starcounter.com/internal/wiki/W3">
    /// here</a>.
    /// </summary>
    public sealed class AssemblySpecification {
        /// <summary>
        /// Allow instantiation only from factory method.
        /// </summary>
        private AssemblySpecification() {
        }

        /// <summary>
        /// Provides the assembly specification class name.
        /// </summary>
        public const string Name = "__starcounterAssemblySpecification";
        
        /// <summary>
        /// Loads the assembly specification from a given assembly.
        /// </summary>
        /// <param name="assembly">The assembly from which to load the
        /// assembly specification.</param>
        /// <returns>An instance representing the assembly specification
        /// found in the given assembly.</returns>
        /// <exception cref="BackingException">
        /// A backing exception indicating an error occured when this
        /// method consumed the backing infrastructure, for example that
        /// an assembly specification was not found. The error code of
        /// the exception, along with any inner exceptions, will describe
        /// more precisely the problem.</exception>
        public static AssemblySpecification LoadFrom(Assembly assembly) {
            throw new NotImplementedException();
        }
    }
}