﻿using PostSharp.Sdk.CodeModel;

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
            if (config.IsConfiguredDatabaseType(typeDef.Name)) {
                if (typeDef.IsPublic()) {
                    return true;
                }
            }

            return IsTaggedWithDatabaseAttribute(typeDef);
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