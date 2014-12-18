// ***********************************************************************
// <copyright file="TypeDef.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System;

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
        /// <remarks>
        /// Obsolete. Will be deprecated and removed in favor of HostedColumns.
        /// </remarks>
        public DbTypeCode[] ColumnRuntimeTypes;

        /// <summary>
        /// Gets the set of hosted columns for the current type.
        /// </summary>
        /// <remarks>
        /// This array is always 1-1 to <see cref="this.TableDef.ColumnDefs"/>.
        /// If a column is indexed there, the same index can be used in this
        /// array to find the corresponding host-specific information.
        /// <para>
        /// Synhronizing the two is the responsibility of the <see cref="Refresh()"/>
        /// method.
        /// </para>
        /// </remarks>
        public HostedColumn[] HostedColumns { 
            get;
            private set; 
        }

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

        public static TypeDef DefineNew(string name, string baseName, TableDef table, TypeLoader typeLoader, PropertyDef[] properties, HostedColumn[] hostedColumns) {
            // TODO: We expect hosted columns to be in sync. Assert that?
            // TODO: Replace public ctor eventually, and complete this method.
            var type = new TypeDef(name, baseName, properties, typeLoader, table, null);
            type.RefreshProperties();
            
            return type;
        }

        /// <summary>
        /// Refresh host-specific instance data held in the current
        /// type according to the underlying table and it's columns.
        /// </summary>
        public void Refresh() {
            RefreshProperties();
            RefreshHostedColumns();
        }

        void RefreshProperties() {
            // Iterate the set of properties that reference a column
            // by name and set their index (based on the column index
            // in the underlying table).
            // This functionality replace LoaderHelper.MapPropertyDefsToColumnDefs.
            foreach (var property in PropertyDefs) {
                if (property.ColumnName != null) {
                    property.ColumnIndex = Array.FindIndex(TableDef.ColumnDefs, candidate => candidate.Name == property.ColumnName);
                    if (property.ColumnIndex == -1) {
                        throw ErrorCode.ToException(
                            Error.SCERRUNEXPDBMETADATAMAPPING, "Column " + property.ColumnName + " cannot be found in ColumnDefs.");
                    }
                }
            }
        }

        void RefreshHostedColumns() {
            var columns = TableDef.ColumnDefs;
            var hostedColumns = new HostedColumn[columns.Length];

            // Make sure the HostedColumns array is resorted so that
            // it match the underlying columns array.
            
            for (int i = 0; i < columns.Length; i++) {
                var column = columns[i];
                var current = HostedColumns.First(candidate => candidate.Name == column.Name);
                hostedColumns[i] = current;
            }

            HostedColumns = hostedColumns;
        }

        #region Metadata-specific helper methods

        // The methods within this region is way to specific for the TypeDef
        // class; they are solely usable when creating definitions for the
        // internal metadata-related .NET types. They should be moved to a
        // more specific space, prefarably in Starcounter.Metadata namespace.

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

        #endregion

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
