using PostSharp.Sdk.CodeModel;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Represents the authority that decide what defined types are to be
    /// considered database types.
    /// </summary>
    internal class DatabaseTypePolicy {
        IType databaseAttributeType;

        public DatabaseTypePolicy(string applicationDirectory, IType databaseAttribute) {
            databaseAttributeType = databaseAttribute;
        }

        public bool IsDatabaseType(TypeDefDeclaration typeDef) {
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