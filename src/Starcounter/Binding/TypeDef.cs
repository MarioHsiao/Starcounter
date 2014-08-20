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
        public PropertyDef[] PropertyDefs {
            get {
                Debug.Assert(_PropertyDefs != null);
                return _PropertyDefs;
            }
            internal set {_PropertyDefs = value;}}

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
        /// This is a help method, which creates TypeDef and TableDef for given meta-data type.
        /// </summary>
        /// <param name="sysType">Instance of System.Type describing the meta-data type.</param>
        /// <returns></returns>
        internal static TypeDef CreateTypeTableDef(System.Type sysType) {
            string typeName = sysType.FullName;
            System.Type baseSysType = sysType.BaseType;
            string baseTypeName = null;
            if (!baseSysType.Equals(typeof(Starcounter.Internal.Entity)))
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
        internal void PopulatePropertyDef() {
#if DEBUG
            if (Name == "Starcounter.Internal.Metadata.MaterializedTable" || 
                Name == "Starcounter.Internal.Metadata.MaterializedColumn" ||
                Name == "Starcounter.Internal.Metadata.MaterializedIndex" ||
                Name == "Starcounter.Internal.Metadata.MaterializedIndexColumn")
                Debug.Assert(_PropertyDefs != null);
            else
                Debug.Assert(_PropertyDefs == null);
#endif
            if (_PropertyDefs == null) {
                TableDef tblDef = Db.LookupTable(Name);
                Debug.Assert(tblDef != null);
                Debug.Assert(TypeInfo.GetType(this.Name).FullName == this.Name);
                PropertyInfo[] properties = TypeInfo.GetType(this.Name).GetProperties(BindingFlags.Instance | BindingFlags.Public);
                Debug.Assert(tblDef.ColumnDefs.Length - 1 == properties.Length);
                PropertyDef[] prpDefs = new PropertyDef[properties.Length];
                DbTypeCode[] typeCodes = new DbTypeCode[tblDef.ColumnDefs.Length];
                typeCodes[0] = DbTypeCode.Key;  // Column 0 is always the key column, __id
                for (int i = 0; i < prpDefs.Length; i++) {
                    DbTypeCode dbTypeCode;
                    if (!System.Enum.TryParse<DbTypeCode>(properties[i].PropertyType.Name, out dbTypeCode)) {
                        dbTypeCode = DbTypeCode.Object;
                        prpDefs[i] = new PropertyDef(properties[i].Name, dbTypeCode,
                            properties[i].PropertyType.FullName);
                    } else
                        prpDefs[i] = new PropertyDef(properties[i].Name, dbTypeCode);
                    prpDefs[i].ColumnName = prpDefs[i].Name;
                    typeCodes[i + 1] = dbTypeCode;
                    int j = 0;
                    while (j < prpDefs.Length && !(prpDefs[i].Name == tblDef.ColumnDefs[j + 1].Name))
                        j++;
                    prpDefs[i].IsNullable = tblDef.ColumnDefs[j + 1].IsNullable;
                    Debug.Assert(prpDefs[i].Name == tblDef.ColumnDefs[j + 1].Name);
                }
                _PropertyDefs = prpDefs;
                TableDef.ColumnDefs = tblDef.ColumnDefs;
                ColumnRuntimeTypes = typeCodes;
            }
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
