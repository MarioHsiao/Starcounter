using Starcounter.Binding;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Starcounter.Internal.Metadata {
    /// <summary>
    /// Builder class that knows how to construct <see cref="TypeDef"/>
    /// instances for metadata classes.
    /// </summary>
    internal class TypeDefBuilder : TypeDef {
        static ColumnDef[] EmptyColumns = new ColumnDef[0];
        static PropertyDef[] EmptyProperties = new PropertyDef[0];

        public TypeDefBuilder(Type t) {
            var name = t.FullName;
            string baseName = null;
            if (!t.BaseType.Equals(typeof(Starcounter.Internal.SystemEntity))) {
                baseName = t.BaseType.FullName;
            }

            Name = name;
            BaseName = baseName;
            TableDef = new TableDef(name, baseName, EmptyColumns);
            PropertyDefs = EmptyProperties;
            TypeLoader = TypeLoader.ForStarcounterType(name);
        }

        public TypeDef BuildFinalTypeDef(int thisIndex, ref TypeDef[] metadataTypeDefs) {
            int baseIndex = -1;
            if (BaseName != null) {
                baseIndex = Array.FindIndex(metadataTypeDefs, candidate => candidate.Name == BaseName);
                var builder = metadataTypeDefs[baseIndex] as TypeDefBuilder;
                if (builder != null) {
                    metadataTypeDefs[baseIndex] = BuildFinalTypeDef(baseIndex, ref metadataTypeDefs);
                }
            }

            // Grab the TableDef that was retrieved based on the layout in
            // the kernel and the set of public properties. This is the info
            // we'll use to construct the TypeDef with.
            var tableDef = Db.LookupTable(Name);
            var properties = TypeInfo.GetType(Name).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            Debug.Assert(tableDef != null);
            Debug.Assert(TypeInfo.GetType(Name).FullName == Name);
            Debug.Assert(tableDef.ColumnDefs.Length - 1 == properties.Length);
            
            var prpDefs = new PropertyDef[properties.Length];
            var hostedColumns = new HostedColumn[tableDef.ColumnDefs.Length];
            hostedColumns[0] = new HostedColumn() { Name = "__id", TypeCode = DbTypeCode.Key };

            // Find and use inherited properties in their order
            int nrInheritedProperties = 0;
            if (BaseName != null) {
                Debug.Assert(baseIndex >= 0 && baseIndex < metadataTypeDefs.Length);
                Debug.Assert(metadataTypeDefs[baseIndex].Name == BaseName);
                TypeDef baseType = metadataTypeDefs[baseIndex];

                Debug.Assert(baseType.PropertyDefs.Length <= prpDefs.Length);
                Debug.Assert(baseType.HostedColumns.Length == baseType.PropertyDefs.Length + 1); // Number of columns is bigger by 1 than number of properties

                for (; nrInheritedProperties < baseType.PropertyDefs.Length; nrInheritedProperties++) {
                    prpDefs[nrInheritedProperties] = baseType.PropertyDefs[nrInheritedProperties];
                    hostedColumns[nrInheritedProperties + 1] = baseType.HostedColumns[nrInheritedProperties + 1];
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
                hostedColumns[1 + curProp] = new HostedColumn() {
                    Name = prpDefs[curProp].ColumnName,
                    TypeCode = dbTypeCode,
                    TargetType = prpDefs[curProp].TargetTypeName
                };
                int j = 1;
                while (j < prpDefs.Length + 1 && !(prpDefs[curProp].Name == tableDef.ColumnDefs[j].Name))
                    j++;
                prpDefs[curProp].IsNullable = tableDef.ColumnDefs[j].IsNullable;
                Debug.Assert(prpDefs[curProp].Name == tableDef.ColumnDefs[j].Name);
            }

            return TypeDef.DefineNew(Name, BaseName, tableDef, TypeLoader, prpDefs, hostedColumns);
        }
    }

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
        internal static TypeDef CreateTypeTableDef(Type sysType) {
            return new TypeDefBuilder(sysType);
        }

        internal static TypeDef[] BuildFinalMetadataTypeDefs(TypeDef[] builders) {
            for (int i = 0; i < builders.Length; i++) {
                var builder = builders[i] as TypeDefBuilder;
                if (builder == null) {
                    Debug.Assert(builders[i] is TypeDef);
                    continue;
                }

                builders[i] = builder.BuildFinalTypeDef(i, ref builders);
            }
            return builders;
        }
    }
}
