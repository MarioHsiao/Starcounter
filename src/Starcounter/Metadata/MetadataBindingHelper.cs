using Starcounter.Binding;
using System.Diagnostics;
using System.Reflection;

namespace Starcounter.Internal.Metadata {
    /// <summary>
    /// Provides a set of helper methods used when binding metadata
    /// CLR classes.
    /// </summary>
    internal static class MetadataBindingHelper {
        /// <summary>
        /// This is a help method, which creates TypeDef and TableDef for given meta-data type.
        /// </summary>
        /// <param name="sysType">Instance of System.Type describing the meta-data type.</param>
        /// <returns></returns>
        internal static TypeDef CreateTypeTableDef(System.Type sysType) {
            string typeName = sysType.FullName;
            System.Type baseSysType = sysType.BaseType;
            string baseTypeName = null;
            if (!baseSysType.Equals(typeof(Starcounter.Internal.SystemEntity)))
                baseTypeName = baseSysType.FullName;
            string tableName = typeName;
            string baseTableName = baseTypeName;
            var systemTableDef = new TableDef(tableName, baseTableName, null);
            var sysColumnTypeDef = new TypeDef(typeName, baseTypeName, null,
                new TypeLoader(new AssemblyName("Starcounter"), typeName),
                systemTableDef, null);
            return sysColumnTypeDef;
        }

        /// <summary>
        /// Populates properties PropertyDefs, ColumnRuntimeTypes, and TableDef.ColumnDefs
        /// to describe meta-tables, since they cannot be created when TypeDef is created
        /// </summary>
        internal static void PopulatePropertyDef(TypeDef target, TypeDef[] typeDefs) {
            Debug.Assert(target.PropertyDefs == null);
            TableDef tblDef = Db.LookupTable(target.Name);
            PropertyInfo[] properties = TypeInfo.GetType(target.Name).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Debug.Assert(tblDef != null);

            Debug.Assert(TypeInfo.GetType(target.Name).FullName == target.Name);
            Debug.Assert(tblDef.ColumnDefs.Length - 1 == properties.Length);

            PropertyDef[] prpDefs = new PropertyDef[properties.Length];
            DbTypeCode[] typeCodes = new DbTypeCode[tblDef.ColumnDefs.Length];
            typeCodes[0] = DbTypeCode.Key;  // Column 0 is always the key column, __id

            // Find and use inherited properties in their order
            int nrInheritedProperties = 0;
            if (target.BaseName != null) {
                // Find Based on typedef
                int based = 0;
                while (based < typeDefs.Length && typeDefs[based].Name != target.BaseName)
                    based++;
                Debug.Assert(based < typeDefs.Length);
                Debug.Assert(typeDefs[based].Name == target.BaseName);
                TypeDef baseType = typeDefs[based];

                Debug.Assert(baseType.PropertyDefs.Length <= prpDefs.Length);
                Debug.Assert(baseType.ColumnRuntimeTypes.Length == baseType.PropertyDefs.Length + 1); // Number of columns is bigger by 1 than number of properties

                for (; nrInheritedProperties < baseType.PropertyDefs.Length; nrInheritedProperties++) {
                    prpDefs[nrInheritedProperties] = baseType.PropertyDefs[nrInheritedProperties];
                    typeCodes[nrInheritedProperties + 1] = baseType.ColumnRuntimeTypes[nrInheritedProperties + 1];
                }
            }

            // Complete with none-inherited properties
            for (int i = 0, curProp = nrInheritedProperties; i < prpDefs.Length - nrInheritedProperties; i++, curProp++) {
                DbTypeCode dbTypeCode;
                if (!System.Enum.TryParse<DbTypeCode>(properties[i].PropertyType.Name, out dbTypeCode)) {
                    dbTypeCode = DbTypeCode.Object;
                    prpDefs[curProp] = new PropertyDef(properties[i].Name, dbTypeCode,
                    properties[i].PropertyType.FullName);
                } else
                    prpDefs[curProp] = new PropertyDef(properties[i].Name, dbTypeCode);
                prpDefs[curProp].ColumnName = prpDefs[curProp].Name;
                typeCodes[1 + curProp] = dbTypeCode;
                int j = 1;
                while (j < prpDefs.Length + 1 && !(prpDefs[curProp].Name == tblDef.ColumnDefs[j].Name))
                    j++;
                prpDefs[curProp].IsNullable = tblDef.ColumnDefs[j].IsNullable;
                Debug.Assert(prpDefs[curProp].Name == tblDef.ColumnDefs[j].Name);
            }
            target.PropertyDefs = prpDefs;
            target.TableDef.ColumnDefs = tblDef.ColumnDefs;
            target.ColumnRuntimeTypes = typeCodes;
        }
    }
}
