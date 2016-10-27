using PostSharp.Sdk.CodeModel;
using Starcounter.Advanced;
using System.Linq;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Represents the authority that decide what defined types are to be
    /// considered database types.
    /// </summary>
    internal class DatabaseTypePolicy {
        DatabaseTypeConfiguration config;
        IType databaseAttributeType;
        IType transientAttributeType;

        /// <summary>
        /// Gets the underlying <see cref="DatabaseTypeConfiguration"/> influencing
        /// the current policy.
        /// </summary>
        public DatabaseTypeConfiguration Configuration {
            get {
                return config;
            }
        }

        public DatabaseTypePolicy(DatabaseTypeConfiguration typeConfiguration, IType databaseAttribute, IType transientAttribute) {
            config = typeConfiguration;
            databaseAttributeType = databaseAttribute;
            transientAttributeType = transientAttribute;
        }

        public bool IsDatabaseType(TypeDefDeclaration typeDef) {
            // Explicit transient attribute overrides everything else
            var transient = IsTaggedWithTransientAttribute(typeDef);
            if (transient) return false;

            // For public types, we allow them to be database classes by
            // means of configuration / assembly level constructs. Check
            // that first.
            if (typeDef.IsPublic()) {
                if (IsInDatabaseAttributeAssembly(typeDef)) {
                    return true;
                }

                if (config.IsConfiguredDatabaseType(typeDef.Name)) {
                    return true;
                }
            }
            
            // Finally the standard path
            return IsTaggedWithDatabaseAttribute(typeDef);
        }

        public bool ImplementSetValueCallback(TypeDefDeclaration typeDef) {
            var name = typeof(ISetValueCallback).FullName;
            var cursor = typeDef;

            while (!IsImplementingInterface(cursor, name)) {
                if (cursor.BaseType != null) {
                    cursor = cursor.BaseType.GetTypeDefinition();
                } else {
                    return false;
                }
            }
            return true;
        }

        bool IsImplementingInterface(TypeDefDeclaration typeDef, string interfaceName) {
            return typeDef.InterfaceImplementations.Any((candidate) => {
                return candidate.ImplementedInterface.GetReflectionName() == interfaceName;
            });
        }

        bool IsInDatabaseAttributeAssembly(TypeDefDeclaration typeDef) {
            return typeDef.Module.AssemblyManifest.CustomAttributes.Contains(databaseAttributeType);
        }

        bool IsTaggedWithDatabaseAttribute(TypeDefDeclaration typeDef) {
            var cursor = typeDef;
            while (!cursor.CustomAttributes.Contains(databaseAttributeType)) {
                if (cursor.BaseType != null) {
                    cursor = cursor.BaseType.GetTypeDefinition();
                } else {
                    return false;
                }
            }
            return true;
        }

        bool IsTaggedWithTransientAttribute(TypeDefDeclaration typeDef) {
            var cursor = typeDef;
            while (!TypeOrInterfacesTransient(typeDef)) {
                if (cursor.BaseType != null) {
                    cursor = cursor.BaseType.GetTypeDefinition();
                } else {
                    return false;
                }
            }
            return true;
        }

        bool TypeOrInterfacesTransient(TypeDefDeclaration typeDef) {
            var transient = typeDef.CustomAttributes.Contains(transientAttributeType);
            if (!transient) {
                var interfaces = typeDef.InterfaceImplementations;
                if (interfaces != null) {
                    transient = interfaces.Any((i) => {
                        var interfaceType = i.ImplementedInterface.GetTypeDefinition(BindingOptions.Default | BindingOptions.DontThrowException);
                        return interfaceType != null && interfaceType.CustomAttributes.Contains(transientAttributeType);
                    });
                }
            }
            return transient;
        }
    }
}