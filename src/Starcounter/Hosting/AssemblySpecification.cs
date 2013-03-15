
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represents the interface to a assembly specification, as it is
    /// defined <a href="http://www.starcounter.com/internal/wiki/W3">
    /// here</a>.
    /// </summary>
    public sealed class AssemblySpecification {
        Type specificationType;
        Type databaseClassIndexType;

        /// <summary>
        /// Allow instantiation only from factory method.
        /// </summary>
        private AssemblySpecification(Type specType, Type dbClassIndexType) {
            this.specificationType = specType;
            databaseClassIndexType = dbClassIndexType;
        }

        /// <summary>
        /// Provides the assembly specification class name.
        /// </summary>
        public const string Name = "__starcounterAssemblySpecification";

        /// <summary>
        /// Provides the name of the database class index type.
        /// </summary>
        public const string DatabaseClassIndexName = "__databaseClasses";
        
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
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            string specName = AssemblySpecification.Name;
            string indexName = AssemblySpecification.DatabaseClassIndexName;
            string msg;
            try {
                var specType = assembly.GetType(AssemblySpecification.Name);
                if (specType == null) {
                    msg = string.Format("Specification \"{0}\" not found in assembly \"{1}.",
                        specName, assembly.FullName
                        );
                    throw ErrorCode.ToException(Error.SCERRASSEMBLYSPECNOTFOUND, msg);
                }

                var dbIndexType = specType.GetNestedType(indexName);
                if (dbIndexType == null) {
                    msg = string.Format("Index type \"{0}\" not found in assembly specification \"{1}\" of assembly \"{2}.",
                        indexName,
                        specName,
                        assembly.FullName
                        );
                    throw ErrorCode.ToException(Error.SCERRBACKINGDBINDEXTYPENOTFOUND, msg);
                }

                return new AssemblySpecification(specType, dbIndexType);

            } catch (Exception e) {
                msg = string.Format("Specification \"{0}\", Assembly = \"{1}\"", specName, assembly.FullName);
                throw ErrorCode.ToException(Error.SCERRBACKINGRETREIVALFAILED, e, msg);
            }
        }

        /// <summary>
        /// Gets the types in the database class index.
        /// </summary>
        /// <returns>An array that contains all the types indexed by the
        /// database class index.</returns>
        public Type[] GetDatabaseClasses() {
            var types = new List<Type>();
            
            // The current implementation uses fields in the database index
            // class to index whatever database types is identified in the
            // assembly of the particular database class index. Hence, we just
            // get all fields (and assert they are typed with a Type field
            // type.

            try {
                var fields = databaseClassIndexType.GetFields();
                foreach (var field in fields) {
                    Trace.Assert(field.FieldType == typeof(Type));
                    types.Add(field.FieldType);
                }
            } catch (Exception e) {
                var msg = "Failed getting types from database class index. Specification \"{0}\", Database Class Index \"{1}\" Assembly = \"{2}\"";
                msg = string.Format(
                    msg,
                    specificationType.Name,
                    databaseClassIndexType.Name,
                    databaseClassIndexType.Assembly.FullName
                    );
                throw ErrorCode.ToException(Error.SCERRBACKINGRETREIVALFAILED, e, msg);
            }

            return types.ToArray();
        }
    }
}