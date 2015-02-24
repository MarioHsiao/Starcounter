
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
                    result.Add(typeRef, typeRef.AttributeType as DatabaseEntityClass);
                }
            }
            return result;
        }

        public static List<DatabaseEntityClass> GetCompileTimeTypes(this DatabaseSchema schema) {
            var result = new List<DatabaseEntityClass>();
            var refs = GetCompileTimeTypeReferences(schema);
            result.AddRange(refs.Values.Distinct());
            return result;
        }
    }
}