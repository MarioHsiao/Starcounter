
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace Starcounter.Hosting {
    internal static class __Assert {
        internal static void OrThrow(bool condition, uint errorCode, string message) {
#if TRACE
            if (!condition) {
                throw ErrorCode.ToException(errorCode, message);
            }
#endif
        }
    }

    /// <summary>
    /// Represents the interface to a assembly specification, as it is
    /// defined <a href="http://www.starcounter.com/internal/wiki/W3">
    /// here</a>.
    /// </summary>
    public sealed class AssemblySpecification {
        Type specificationType;
        Type databaseClassIndexType;
        Dictionary<Type, Type> databaseTypeToSpecType;

        /// <summary>
        /// Allow instantiation only from factory method.
        /// </summary>
        private AssemblySpecification(Type specType, Type dbClassIndexType) {
            this.specificationType = specType;
            databaseClassIndexType = dbClassIndexType;
            databaseTypeToSpecType = null;
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

                var dbIndexType = specType.GetNestedType(indexName, BindingFlags.NonPublic);
                if (dbIndexType == null) {
                    msg = string.Format("Index type \"{0}\" not found in assembly specification \"{1}\" of assembly \"{2}.",
                        indexName,
                        specName,
                        assembly.FullName
                        );
                    throw ErrorCode.ToException(Error.SCERRBACKINGDBINDEXTYPENOTFOUND, msg);
                }

                return new AssemblySpecification(specType, dbIndexType);

            } catch (BackingException) {
                throw;

            } catch (Exception e) {
                if (ErrorCode.IsFromErrorCode(e))
                    throw;

                msg = string.Format("Specification \"{0}\", Assembly = \"{1}\"", specName, assembly.FullName);
                throw ErrorCode.ToException(Error.SCERRBACKINGRETREIVALFAILED, e, msg);
            }
        }

        /// <summary>
        /// Gets the database types in the database class index.
        /// </summary>
        /// <returns>An array that contains all the types indexed by
        /// the database class index.
        /// </returns>
        public Type[] GetDatabaseClasses() {
            if (databaseTypeToSpecType == null) {
                CacheDatabaseClassIndex();
            }
            
            return databaseTypeToSpecType.Keys.ToArray();
        }

        /// <summary>
        /// Gets all type specification types in the database class
        /// index.
        /// </summary>
        /// <returns>An array that contains all the types indexed by
        /// the database class index. Each returned type is a type
        /// specification.
        /// </returns>
        public Type[] GetDatabaseTypeSpecifications() {
            if (databaseTypeToSpecType == null) {
                CacheDatabaseClassIndex();
            }

            return databaseTypeToSpecType.Values.ToArray();
        }

        /// <summary>
        /// Gets a <see cref="TypeSpecification"/> instance, providing
        /// access to the recorded metadata of the given database type.
        /// </summary>
        /// <param name="databaseClassType">The database type whose
        /// database specific metadata to provide access to.</param>
        /// <returns>A <see cref="TypeSpecification"/> that can be used
        /// to consume database specific metadata about the given type.
        /// </returns>
        /// <example>
        /// var spec = GetSpecificationOf(typeof(Person));
        /// </example>
        public TypeSpecification GetSpecificationOf(Type databaseClassType) {
            Type specType;

            if (databaseClassType == null) {
                throw new ArgumentNullException("databaseClassType");
            }

            if (databaseTypeToSpecType == null) {
                CacheDatabaseClassIndex();
            }

            specType = databaseTypeToSpecType[databaseClassType];
            return new TypeSpecification(specType);
        }

        void CacheDatabaseClassIndex() {
            // The current implementation uses fields in the database index
            // class to index whatever database types is identified in the
            // assembly of the particular database class index. Hence, we just
            // get all fields (and assert they are typed with a Type field
            // type.

            lock (this) {
                var matchingFields = new List<FieldInfo>();
                try {
                    var fields = databaseClassIndexType.GetFields();
                    foreach (var field in fields) {
                        // Let's not use any error code for this, just assert it,
                        // at least as long as we haven't ruled out that we might
                        // want to stuff other metadata into the database class
                        // index.
                        __Assert.OrThrow(
                            field.FieldType == typeof(Type),
                            Error.SCERRBACKINGRETREIVALFAILED,
                            "Fields in the database index should be declared with the System.Type field type."
                            );
                        var referencedSpecification = field.GetValue(null) as Type;
                        __Assert.OrThrow(
                            referencedSpecification != null,
                            Error.SCERRBACKINGRETREIVALFAILED,
                            string.Format("Fields in the database class index must be assigned, field {0} is not.", field.Name)
                            );
                        __Assert.OrThrow(
                            referencedSpecification.Name.EndsWith(TypeSpecification.Name),
                            Error.SCERRBACKINGRETREIVALFAILED,
                            string.Format("Fields must reference a type named *.{0}", TypeSpecification.Name)
                            );

                        matchingFields.Add(field);
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

                databaseTypeToSpecType = new Dictionary<Type, Type>(matchingFields.Count);
                foreach (var spec in matchingFields) {
                    var specificationType = spec.GetValue(null) as Type;
                    databaseTypeToSpecType.Add(specificationType.DeclaringType, specificationType);
                }
            }
        }
    }
}