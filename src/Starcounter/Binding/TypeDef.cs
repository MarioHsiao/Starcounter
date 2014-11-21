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
            internal set {
                _PropertyDefs = value;
                AssignDymanicTypeInformation();
            }
        }

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
        /// Gets the index of the property that references the (dynamic)
        /// type (if any) of the current TypeDef. Set to -1 when no such
        /// information is available.
        /// </summary>
        public int TypePropertyIndex { get; set; }

        /// <summary>
        /// Gets the index of the property that references the (dynamic)
        /// based type (if any) of the current TypeDef. Set to -1 when no such
        /// information is available.
        /// </summary>
        public int InheritsPropertyIndex { get; set; }

        /// <summary>
        /// Gets the index of the property that references the (dynamic)
        /// type name (if any) of the current TypeDef. Set to -1 when no such
        /// information is available.
        /// </summary>
        public int TypeNameIndex { get; set; }

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
        internal void PopulatePropertyDef(TypeDef[] typeDefs) {
            Debug.Assert(_PropertyDefs == null);
            TableDef tblDef = Db.LookupTable(Name);
                PropertyInfo[] properties = TypeInfo.GetType(this.Name).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Debug.Assert(tblDef != null);

            Debug.Assert(TypeInfo.GetType(this.Name).FullName == this.Name);
            Debug.Assert(tblDef.ColumnDefs.Length - 1 == properties.Length);
                
            PropertyDef[] prpDefs = new PropertyDef[properties.Length];
            DbTypeCode[] typeCodes = new DbTypeCode[tblDef.ColumnDefs.Length];
            typeCodes[0] = DbTypeCode.Key;  // Column 0 is always the key column, __id

                // Find and use inherited properties in their order
                int nrInheritedProperties = 0;
                if (BaseName != null) {
                    // Find Based on typedef
                    int based = 0;
                    while (based < typeDefs.Length && typeDefs[based].Name != BaseName)
                        based++;
                    Debug.Assert(based < typeDefs.Length);
                    Debug.Assert(typeDefs[based].Name == BaseName);
                    TypeDef baseType = typeDefs[based];

                    Debug.Assert(baseType.PropertyDefs.Length <= prpDefs.Length);
                    Debug.Assert(baseType.ColumnRuntimeTypes.Length == baseType.PropertyDefs.Length + 1); // Number of columns is bigger by 1 than number of properties

                    for (; nrInheritedProperties < baseType.PropertyDefs.Length; nrInheritedProperties++) {
                        prpDefs[nrInheritedProperties] = baseType.PropertyDefs[nrInheritedProperties];
                        typeCodes[nrInheritedProperties+1] = baseType.ColumnRuntimeTypes[nrInheritedProperties+1];
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
                _PropertyDefs = prpDefs;
                TableDef.ColumnDefs = tblDef.ColumnDefs;
                ColumnRuntimeTypes = typeCodes;
            }

        internal IndexInfo2 GetIndexInfo(string name) {
            var indexInfo = TableDef.GetIndexInfo(name);
            if (indexInfo != null) {
                return new IndexInfo2(indexInfo, this);
            }
            return null;
        }

        /// <summary>
        /// Invoked every time _PropertyDefs is assigned. Reevaluates
        /// dynamic type information for the current TypeDef.
        /// </summary>
        void AssignDymanicTypeInformation() {
            TypeNameIndex = TypePropertyIndex = InheritsPropertyIndex = -1;
            if (_PropertyDefs != null) {
                for (int i = 0; i < _PropertyDefs.Length; i++) {
                    var candidate = _PropertyDefs[i];
                    if (candidate.IsTypeReference) {
                        TypePropertyIndex = i;
                    } else if (candidate.IsInheritsReference) {
                        InheritsPropertyIndex = i;
                    } else if (candidate.IsTypeName) {
                        TypeNameIndex = i;
                    }
                }
            }
        }
    }
}
