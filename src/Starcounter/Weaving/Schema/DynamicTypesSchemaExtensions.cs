
using System.Collections.Generic;
using System.Linq;

namespace Sc.Server.Weaver.Schema {

    public static class DynamicTypesSchemaExtensions {

        public static Dictionary<DatabaseAttribute, DatabaseEntityClass> GetCompileTimeTypeReferences(
            this DatabaseSchema schema) {

            var result = new Dictionary<DatabaseAttribute, DatabaseEntityClass>();
            foreach (var dbc in schema.IndexedDatabaseClasses) {
                var typeRef = dbc.Attributes.FirstOrDefault(a => a.IsTypeReference);
                if (typeRef != null) {
                    var target = typeRef.AttributeType as DatabaseEntityClass;
                    if (target != null && !target.IsEntityClass) {
                        result.Add(typeRef, typeRef.AttributeType as DatabaseEntityClass);
                    }
                }
            }
            return result;
        }

        public static List<DatabaseEntityClass> GetCompileTimeTypes(this DatabaseSchema schema) {
            var explicitTypes = new List<DatabaseEntityClass>();
            var refs = GetCompileTimeTypeReferences(schema);
            explicitTypes.AddRange(refs.Values.Distinct());

            var implicitTypes = new List<DatabaseEntityClass>();
            foreach (var c in schema.IndexedDatabaseClasses) {
                // if c is either the parent of a type, or a subclass
                // of a type, it must be a type itself
                if (explicitTypes.Contains(c) || c.IsEntityClass || c.IsImplicitEntityClass) {
                    continue;
                }

                var typeInHierarchy = explicitTypes.FirstOrDefault((candidate) => {
                    return c.Inherit(candidate);
                });
                if (typeInHierarchy != null) {
                    implicitTypes.Add(c as DatabaseEntityClass);
                }
            }

            explicitTypes.AddRange(implicitTypes);
            return explicitTypes;
        }
    }
}