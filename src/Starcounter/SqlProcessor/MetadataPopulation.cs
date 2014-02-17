using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace Starcounter.SqlProcessor {
    public static class MetadataPopulation {
        public static void PopulateClrViewsMetaData(TypeDef[] typeDefs) {
            foreach (TypeDef typeDef in typeDefs) {
                string typeName = typeDef.Name.LastDotWord();
                string assemblyName = "";
                try {
                    string assemblyPath = Application.Current.FilePath;
                    assemblyName = '.' + assemblyPath.Substring(assemblyPath.LastIndexOf('\\'));
                } catch (InvalidOperationException) { }
                string classReverseFullName = typeDef.Name.ReverseOrderDotWords();
                string fullName = classReverseFullName + assemblyName + '.' + AppDomain.CurrentDomain.FriendlyName;
                string[] propertyNames = new string[typeDef.PropertyDefs.Length];
                ushort[] dbTypes = new ushort[typeDef.PropertyDefs.Length];
                string[] columnNames = new string[typeDef.PropertyDefs.Length];
                string[] codePropertyNames = new string[typeDef.PropertyDefs.Length];
                int nrCols = 0;
                int nrCodeprops = 0;
                for (int i = 0; i < typeDef.PropertyDefs.Length; i++) {
                    if (typeDef.PropertyDefs[i].ColumnName == null) {
                        codePropertyNames[nrCodeprops] = typeDef.PropertyDefs[i].Name;
                        nrCodeprops++;
                    } else {
                        propertyNames[nrCols] = typeDef.PropertyDefs[i].Name;
                        dbTypes[nrCols] = (ushort)typeDef.PropertyDefs[i].Type;
                        columnNames[nrCols] = typeDef.PropertyDefs[i].ColumnName;
                        nrCols++;
                    }
                }
                Debug.Assert(nrCodeprops + nrCols <= typeDef.PropertyDefs.Length);
                Starcounter.SqlProcessor.SqlProcessor.PopulateAClrView(typeName, fullName, typeDef.Name, typeDef.BaseName,
                    propertyNames, dbTypes, typeDef.TableDef.Name, columnNames);
            }
        }
    }
}
