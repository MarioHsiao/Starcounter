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

        /// <summary>
        /// Gets the underlying <see cref="DatabaseTypeConfiguration"/> influencing
        /// the current policy.
        /// </summary>
        public DatabaseTypeConfiguration Configuration {
            get {
                return config;
            }
        }

        public DatabaseTypePolicy(DatabaseTypeConfiguration typeConfiguration, IType databaseAttribute) {
            config = typeConfiguration;
            databaseAttributeType = databaseAttribute;
        }

        public bool IsDatabaseType(TypeDefDeclaration typeDef) {
            // The order here is important: we must allow the user to fine
            // tune by explicit tagging, so check that first.
            var tagged = IsTaggedWithDatabaseAttribute(typeDef);
            if (tagged) return true;

            // We should support transient on database types to allow
            // them to be part of a assembly tagged or configured, but
            // to override the default, and become not considered database
            // classes (even if public)
            // TODO:

            if (IsInDatabaseAttributeAssembly(typeDef)) {
                if (typeDef.IsPublic()) {
                    return true;
                }
            }

            if (config.IsConfiguredDatabaseType(typeDef.Name)) {
                if (typeDef.IsPublic()) {
                    return true;
                }
            }

            return false;
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
    }
}