
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
        /// Converts the (full) name of a .NET type to the form used in
        /// the database class index when referencing it.
        /// </summary>
        /// <remarks>
        /// Even though .NET support naming fields with dots and plus signs,
        /// the C# language does not and we have the ambition to allow any
        /// assembly specification to be manually crafted in C# (primarily
        /// for the sake of testing).
        /// </remarks>
        /// <seealso cref="ClassIndexNameToTypeName"/>
        /// <param name="reflectedName">The full name of the .NET type.</param>
        /// <returns>The name as used in the class index.</returns>
        public static string TypeNameToClassIndexName(string reflectedName) {
            if (reflectedName == null)
                throw new ArgumentNullException("reflectedName");

            return reflectedName.Replace(".", "_").Replace("+", "__");
        }

        /// <summary>
        /// Converts a name of a database class from the form used in the
        /// database class index to it's .NET equivalent.
        /// </summary>
        /// <remarks>
        /// Even though .NET support naming fields with dots and plus signs,
        /// the C# language does not and we have the ambition to allow any
        /// assembly specification to be manually crafted in C# (primarily
        /// for the sake of testing).
        /// </remarks>
        /// <seealso cref="TypeNameToClassIndexName"/>
        /// <param name="classIndexName">The name as used in the class index.
        /// </param>
        /// <returns>The full name of the .NET type.</returns>
        public static string ClassIndexNameToTypeName(string classIndexName) {
            if (classIndexName == null)
                throw new ArgumentNullException("classIndexName");

            return classIndexName.Replace("__", "+").Replace("_", ".");
        }
        
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
                var specType = assembly.GetType(AssemblySpecification.Name, false);
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

            var assembly = specificationType.Assembly;
            lock (this) {
                var specificationsFound = new List<Type>();
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
                        // Why would we need the reference assigned when we know
                        // that we can get the type from the same assembly just be
                        // doing assembly.GetType(fieldName);
                        // TODO:

                        // By converting using the name of the reference (after some
                        // formatting), we should be able to get the database type
                        // itself.

                        var databaseTypeName = ClassIndexNameToTypeName(field.Name);
                        var databaseType = assembly.GetType(databaseTypeName, false);
                        __Assert.OrThrow(
                            databaseType != null,
                            Error.SCERRBACKINGRETREIVALFAILED,
                            string.Format("Unable to resolve database type {0}, referenced in class index using {1}", databaseTypeName, field.Name)
                            );
                        var typeSpecType = databaseType.GetNestedType(TypeSpecification.Name, BindingFlags.NonPublic);
                        __Assert.OrThrow(
                            typeSpecType != null,
                            Error.SCERRBACKINGRETREIVALFAILED,
                            string.Format("Unable to find nested type specification of database type {0}, using {1}", databaseType, TypeSpecification.Name)
                            );

                        specificationsFound.Add(typeSpecType);
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

                databaseTypeToSpecType = new Dictionary<Type, Type>(specificationsFound.Count);
                foreach (var spec in specificationsFound) {
                    databaseTypeToSpecType.Add(spec.DeclaringType, spec);
                }
            }
        }
    }
}