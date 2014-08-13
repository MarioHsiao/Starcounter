using PostSharp.Sdk.CodeModel;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Represents the authority that decide what defined types are to be
    /// considered database types.
    /// </summary>
    internal class DatabaseTypePolicy {
        DatabaseTypeConfiguration config;
        IType databaseAttributeType;

        public DatabaseTypePolicy(string applicationDirectory, IType databaseAttribute) {
            config = DatabaseTypeConfiguration.Open(applicationDirectory);
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