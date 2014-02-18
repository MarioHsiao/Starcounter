using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Starcounter.SqlProcessor {
    public static class MetadataPopulation {
        public static unsafe void PopulateClrViewsMetaData(TypeDef[] typeDefs) {
            foreach (TypeDef typeDef in typeDefs) {
                string typeName = typeDef.Name.LastDotWord();
                string assemblyName = "";
                try {
                    string assemblyPath = Application.Current.FilePath;
                    assemblyName = '.' + assemblyPath.Substring(assemblyPath.LastIndexOf('\\'));
                } catch (InvalidOperationException) { }
                string classReverseFullName = typeDef.Name.ReverseOrderDotWords();
                string fullName = classReverseFullName + assemblyName + '.' + AppDomain.CurrentDomain.FriendlyName;
                char*[] propertyNames = new char*[typeDef.PropertyDefs.Length];
                ushort[] dbTypes = new ushort[typeDef.PropertyDefs.Length];
                char*[] columnNames = new char*[typeDef.PropertyDefs.Length];
                char*[] codePropertyNames = new char*[typeDef.PropertyDefs.Length];
                ushort nrCols = 0;
                ushort nrCodeprops = 0;
                for (int i = 0; i < typeDef.PropertyDefs.Length; i++) {
                    if (typeDef.PropertyDefs[i].ColumnName == null) {
                        codePropertyNames[nrCodeprops] = (char*)Marshal.StringToCoTaskMemUni(typeDef.PropertyDefs[i].Name);
                        nrCodeprops++;
                    } else {
                        propertyNames[nrCols] = (char*)Marshal.StringToCoTaskMemUni(typeDef.PropertyDefs[i].Name);
                        dbTypes[nrCols] = (ushort)typeDef.PropertyDefs[i].Type;
                        columnNames[nrCols] = (char*)Marshal.StringToCoTaskMemUni(typeDef.PropertyDefs[i].ColumnName);
                        nrCols++;
                    }
                }
                Debug.Assert(nrCodeprops + nrCols <= typeDef.PropertyDefs.Length);
                ClrView aView;
                aView.TypeName = (char*)Marshal.StringToCoTaskMemUni(typeName);
                aView.FullName = (char*)Marshal.StringToCoTaskMemUni(fullName);
                aView.FullClassName = (char*)Marshal.StringToCoTaskMemUni(typeDef.Name);
                aView.ParentTypeName = (char*)Marshal.StringToCoTaskMemUni(typeDef.BaseName);
                aView.TableName = (char*)Marshal.StringToCoTaskMemUni(typeDef.TableDef.Name);
                aView.NrProperties = nrCols;
                aView.NrCodeProperties = nrCodeprops;
                fixed (UInt16* dbTypesPtr = dbTypes)
                fixed (char** properyNamesPtr = propertyNames, columnNamesPtr = columnNames,
                    codePropertyNamesPtr = codePropertyNames) {
                    aView.PropertyNames = properyNamesPtr;
                    aView.CodePropertyNames = codePropertyNamesPtr;
                    aView.ColumnNames = columnNamesPtr;
                    aView.DbTypes = dbTypesPtr;
                    uint err = Starcounter.SqlProcessor.SqlProcessor.scsql_populate_clrview(&aView);
                    if (err != 0)
                        throw ErrorCode.ToException(err);
                }
            }
        }
    }
}
