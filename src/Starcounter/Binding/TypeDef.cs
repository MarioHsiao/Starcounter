// ***********************************************************************
// <copyright file="TypeDef.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System;
using Starcounter.Internal;

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
        /// Gets the set of hosted columns for the current type.
        /// </summary>
        /// <remarks>
        /// This array is always 1-1 to <c>TableDef.ColumnDefs</c>.
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
        /// Gets a value that indicates if the current type is defined by
        /// Starcounter.
        /// </summary>
        public bool IsStarcounterType {
            get {
                return TypeLoader.LoadsFromStarcounterAssembly;
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
        /// Raw object reference to a resolved dynamic type in the form of a
        /// <see cref="ObjectRef"/>. If no dynamic type has been established
        /// for the current type, this reference is invalid.
        /// </summary>
        /// <remarks>Will be initialized as part of binding.</remarks>
        public ObjectRef RuntimeDefaultTypeRef;

        private TypeDef(
            string name,
            string baseName,
            TableDef tableDef,
            PropertyDef[] properties,
            HostedColumn[] hostedColumns,
            TypeLoader typeLoader) : this() {
            Name = name;
            TableDef = tableDef;
            BaseName = baseName;
            PropertyDefs = properties;
            TypeLoader = typeLoader;
            HostedColumns = hostedColumns;
        }

        protected TypeDef() {
            RuntimeDefaultTypeRef = new ObjectRef();
            RuntimeDefaultTypeRef.InitInvalid();
        }

        public static TypeDef DefineNew(string name, string baseName, TableDef table, TypeLoader typeLoader, PropertyDef[] properties, HostedColumn[] hostedColumns) {
            var type = new TypeDef(name, baseName, table, properties, hostedColumns, typeLoader);
            type.Refresh();
            Trace.Assert(type.AssertHostedColumnsAreInSynchWithColumns());
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

        /// <summary>
        /// Gets the (dynamic) type object that is considered the
        /// default for the current type definition (established at
        /// bind time).
        /// </summary>
        /// <returns>The type object of the current type.</returns>
        public IObjectProxy GetTypeObject() {
            Trace.Assert(RuntimeDefaultTypeRef.ObjectID != 0);
            return (IObjectProxy) DbHelper.FromID(RuntimeDefaultTypeRef.ObjectID);
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

        bool AssertHostedColumnsAreInSynchWithColumns() {
            var hosted = HostedColumns;
            var columns = TableDef.ColumnDefs;
            var result = hosted.Length == columns.Length;
            if (result) {
                result = hosted.All((hc) => {
                    return Array.FindIndex(hosted, candidate => candidate.Name == hc.Name) ==
                        Array.FindIndex(columns, candidate => candidate.Name == hc.Name);
                });
            }
            return result;
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
