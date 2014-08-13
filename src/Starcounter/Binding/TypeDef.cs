// ***********************************************************************
// <copyright file="TypeDef.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class TypeDef
    /// </summary>
    public class TypeDef
    {

        /// <summary>
        /// The name
        /// </summary>
        public string Name;

        /// <summary>
        /// The base name
        /// </summary>
        public string BaseName;

        /// <summary>
        /// The property defs
        /// </summary>
        private PropertyDef[] _PropertyDefs;
        private bool _set = false;
        public PropertyDef[] PropertyDefs {
            get {
                if(_set)
                unsafe {
                    TableDef tblDef = Db.LookupTable(Name);
                    Debug.Assert(Db.LookupTable(Name)!=null);
                    Debug.Assert(tblDef.ColumnDefs.Length == TableDef.ColumnDefs.Length);
                    PropertyDef[] prpDef = new PropertyDef[tblDef.ColumnDefs.Length - 1];
                    for (int i = 0; i < prpDef.Length; i++) {
                        //prpDef[i] = new PropertyDef(tblDef.ColumnDefs[i + 1].Name);
                    }
#if DEBUG
                    for (int i = 0; i < tblDef.ColumnDefs.Length; i++) {
                        Debug.Assert(tblDef.ColumnDefs[i].Name == TableDef.ColumnDefs[i].Name);
                        Debug.Assert(tblDef.ColumnDefs[i].Type == TableDef.ColumnDefs[i].Type);
                        Debug.Assert(tblDef.ColumnDefs[i].IsNullable == TableDef.ColumnDefs[i].IsNullable);
                        Debug.Assert(tblDef.ColumnDefs[i].IsInherited == TableDef.ColumnDefs[i].IsInherited);
                    }
#endif
                }
                return _PropertyDefs;
            }
            set {_PropertyDefs = value;}}

        /// <summary>
        /// The type loader
        /// </summary>
        public TypeLoader TypeLoader;

        /// <summary>
        /// The table def
        /// </summary>
        public TableDef TableDef;

        /// <summary>
        /// Array parallel to TableDef.ColumnDefs with type codes of columns as mapped by
        /// properties.
        /// </summary>
        public DbTypeCode[] ColumnRuntimeTypes;

        private string LowerName_;
        /// <summary>
        /// Gets the full name of the class in lowercase.
        /// </summary>
        public string LowerName {
            get {
                if (LowerName_ == null)
                    LowerName_ = Name.ToLower();
                return LowerName_;
            }
        }

        private string ShortName_;
        /// <summary>
        /// Gets the class name without namespaces in lowercase.
        /// </summary>
        public string ShortName {
            get {
                if (ShortName_ == null) {
                    int pos = Name.LastIndexOf('.');
                    ShortName_ = pos == -1 ? Name.ToLower() : Name.Substring(pos + 1).ToLower();
                }
                return ShortName_;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDef" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="baseName">Name of the base.</param>
        /// <param name="propertyDefs">The property defs.</param>
        /// <param name="typeLoader">The type loader.</param>
        /// <param name="tableDef">The table def.</param>
        public TypeDef(string name, string baseName, PropertyDef[] propertyDefs, TypeLoader typeLoader, TableDef tableDef, DbTypeCode[] columnRuntimeTypes)
        {
            Name = name;
            BaseName = baseName;
            PropertyDefs = propertyDefs;
            TypeLoader = typeLoader;
            TableDef = tableDef;
            ColumnRuntimeTypes = columnRuntimeTypes;
        }

        /// <summary>
        /// This is a help method, which creates TypeDef and TableDef for given names and columns/properties. Some property information are filled 
        /// from columns, thus it is assumed that columns and properties have the same order.
        /// </summary>
        /// <param name="typeName">The name of the type to create TypeDef</param>
        /// <param name="baseTypeName">The name of the base type or null</param>
        /// <param name="tableName">The name of the table to which type is mapped and for which TableDef to create.</param>
        /// <param name="baseTableName">The name of the base table</param>
        /// <param name="columnDefs">Complete ColumnDef for the TableDef</param>
        /// <param name="propDefs">PropertyDef with names and types of the properties with the same
        /// order as ColumnDef. Column names and nullable properties are extracted from columnDefs</param>
        /// <returns></returns>
        internal static TypeDef CreateTypeTableDef(System.Type sysType, 
            ColumnDef[] columnDefs, PropertyDef[] propDefs) {
            string typeName = sysType.FullName;
            System.Type baseSysType = sysType.BaseType;
            string baseTypeName = null;
            if (!baseSysType.Equals(typeof(Starcounter.Internal.Entity)))
                baseTypeName = baseSysType.FullName;
            string tableName = typeName;
            string baseTableName = baseTypeName;
            var typeCodes = new DbTypeCode[columnDefs.Length];
            typeCodes[0] = DbTypeCode.Key;  // Column 0 is always the key column, __id
            Debug.Assert(propDefs.Length + 1 == columnDefs.Length);
            for (int i = 0; i < propDefs.Length; i++) {
                // Complete PropertyDef from ColumnDef
                propDefs[i].IsNullable = columnDefs[i + 1].IsNullable;
                propDefs[i].ColumnName = columnDefs[i + 1].Name;
                // Populate TypeCodes
                typeCodes[i + 1] = propDefs[i].Type;
            }
            // Create columnDefs and propDefs from stored meta-data through API
#if false
            int colsLength = 0;
            uint err = Starcounter.SqlProcessor.SqlProcessor.scsql_get_metacolumns_length(typeName, &colsLength);
            if (err != 0)
                throw ErrorCode.ToException(err);
            Debug.Assert(colsLength == columnDefs.Length);
#endif
            // Finally create TableDef and TypeDef
            var systemTableDef = new TableDef(tableName, baseTableName, columnDefs);
            var sysColumnTypeDef = new TypeDef(typeName, baseTypeName, propDefs,
                new TypeLoader(new AssemblyName("Starcounter"), typeName),
                systemTableDef, typeCodes);
#if DEBUG
            PropertyInfo[] properties = sysType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Debug.Assert(properties.Length == sysColumnTypeDef.PropertyDefs.Length);
            Debug.Assert(sysColumnTypeDef.TableDef.ColumnDefs.Length == sysColumnTypeDef.PropertyDefs.Length + 1);
            var nrOwnProp = (from col in columnDefs
                             where !col.IsInherited && col.Name != "__id"
                             select col).Count();
            Debug.Assert(nrOwnProp <= properties.Length);
            for (int i = 0; i < properties.Length; i++)
                if (i < nrOwnProp) {
                    Debug.Assert(properties[i].Name == sysColumnTypeDef.PropertyDefs[i + properties.Length - nrOwnProp].Name);
                    Debug.Assert(properties[i].DeclaringType == sysType);
                } else
                    Debug.Assert(properties[i].DeclaringType != sysType);
            if (nrOwnProp < properties.Length)
                Debug.Assert(baseSysType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Length ==
                    properties.Length - nrOwnProp);
            System.Type typeSpec = sysType.GetNestedType("__starcounterTypeSpecification", BindingFlags.NonPublic);
            Debug.Assert(typeSpec != null);
            for (int i = 0; i < sysColumnTypeDef.PropertyDefs.Length; i++) {
                Debug.Assert(sysColumnTypeDef.PropertyDefs[i].Name == sysColumnTypeDef.TableDef.ColumnDefs[i + 1].Name);
                string handleName = "columnHandle_" + sysColumnTypeDef.PropertyDefs[i].Name;
                FieldInfo handleField = typeSpec.GetField(handleName, BindingFlags.Static | BindingFlags.NonPublic);
                Debug.Assert(handleField != null);
                Debug.Assert(handleField.GetValue(null) is int);
                //Debug.Assert((int)handleField.GetValue(null) == i + 1); // Cannot be checked at this moment, since it is set later.
            }
#endif
            sysColumnTypeDef._set = true;
                return sysColumnTypeDef;
        }

        internal IndexInfo2 GetIndexInfo(string name) {
            var indexInfo = TableDef.GetIndexInfo(name);
            if (indexInfo != null) {
                return new IndexInfo2(indexInfo, this);
            }
            return null;
        }
    }
}
