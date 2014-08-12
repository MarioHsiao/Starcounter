using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Reflection;

namespace Starcounter.Metadata {
    public abstract class Table : Type {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_UniqueIdentifierReversed;
            internal static int columnHandle_Inherits;
            internal static int columnHandle_Updatable;
            internal static int columnHandle_UniqueIdentifier;
            internal static int columnHandle_FullName;
        }
#pragma warning disable 0628, 0169
        #endregion

        /// <summary>
        /// Creates the database binding <see cref="TypeDef"/> representing
        /// the type in the database and holding its table- and column defintions.
        /// </summary>
        /// <remarks>
        /// Developer note: if you extend or change this class in any way, make
        /// sure to keep the <see cref="MaterializedColumn.__starcounterTypeSpecification"/>
        /// class in sync with what is returned by this method.
        /// </remarks>
        /// <returns>A <see cref="TypeDef"/> representing the current
        /// type.</returns>
        static new internal TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("UniqueIdentifierReversed", sccoredb.STAR_TYPE_STRING, true, false),
                    new ColumnDef("Inherits", sccoredb.STAR_TYPE_REFERENCE, true, false),
                    new ColumnDef("Updatable", sccoredb.STAR_TYPE_ULONG, false, false),
                    new ColumnDef("UniqueIdentifier", sccoredb.STAR_TYPE_STRING, true, false),
                    new ColumnDef("FullName", sccoredb.STAR_TYPE_STRING, true, false)
                },
                new PropertyDef[] {
                    new PropertyDef("Name", DbTypeCode.String),
                    new PropertyDef("UniqueIdentifierReversed", DbTypeCode.String),
                    new PropertyDef("Inherits", DbTypeCode.Object, "Starcounter.Metadata.Table"),
                    new PropertyDef("Updatable", DbTypeCode.Boolean),
                    new PropertyDef("UniqueIdentifier", DbTypeCode.String),
                    new PropertyDef("FullName", DbTypeCode.String)
                });
        }

        /// <inheritdoc />
        public Table(Uninitialized u)
            : base(u) {
        }

        //internal Table() : this(null) {
        //    DbState.Insert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        //}

        public string UniqueIdentifierReversed {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_UniqueIdentifierReversed); }
            internal set {
                DbState.WriteString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_UniqueIdentifierReversed,
                    value);
            }
        }

        public Table Inherits {
            get {
                return (Table)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Inherits);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Inherits, value);
            }
        }

        public bool Updatable {
            get { return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Updatable); }
            internal set {
                DbState.WriteBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Updatable,
                    value);
            }
        }

        public string UniqueIdentifier {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_UniqueIdentifier); }
            internal set {
                DbState.WriteString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_UniqueIdentifier,
                    value);
            }
        }

        public string FullName {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_FullName); }
            internal set {
                DbState.WriteString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_FullName,
                    value);
            }
        }
    }

    public sealed class RawView : Starcounter.Internal.Metadata.HostMaterializedTable {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_UniqueIdentifierReversed;
            internal static int columnHandle_Inherits;
            internal static int columnHandle_Updatable;
            internal static int columnHandle_UniqueIdentifier;
            internal static int columnHandle_FullName;
            internal static int columnHandle_MaterializedTable;
        }
#pragma warning disable 0628, 0169
        #endregion

        static new internal TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("UniqueIdentifierReversed", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("Inherits", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("Updatable", sccoredb.STAR_TYPE_ULONG, false, true),
                    new ColumnDef("UniqueIdentifier", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("FullName", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("MaterializedTable", sccoredb.STAR_TYPE_REFERENCE, true, true)
                },
                new PropertyDef[] {
                    new PropertyDef("Name", DbTypeCode.String),
                    new PropertyDef("UniqueIdentifierReversed", DbTypeCode.String),
                    new PropertyDef("Inherits", DbTypeCode.Object, "Starcounter.Metadata.Table"),
                    new PropertyDef("Updatable", DbTypeCode.Boolean),
                    new PropertyDef("UniqueIdentifier", DbTypeCode.String),
                    new PropertyDef("FullName", DbTypeCode.String),
                    new PropertyDef("MaterializedTable", DbTypeCode.Object, "Starcounter.Internal.Metadata.MaterializedTable")
                });
        }

        public RawView(Uninitialized u) : base(u) { }
        internal RawView() : this(null) {
            DbState.SystemInsert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }
    }

    public abstract class VMView : Starcounter.Internal.Metadata.HostMaterializedTable {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_UniqueIdentifierReversed;
            internal static int columnHandle_Inherits;
            internal static int columnHandle_Updatable;
            internal static int columnHandle_UniqueIdentifier;
            internal static int columnHandle_FullName;
            internal static int columnHandle_MaterializedTable;
        }
#pragma warning disable 0628, 0169
        #endregion

        static new internal TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("UniqueIdentifierReversed", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("Inherits", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("Updatable", sccoredb.STAR_TYPE_ULONG, false, true),
                    new ColumnDef("UniqueIdentifier", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("FullName", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("MaterializedTable", sccoredb.STAR_TYPE_REFERENCE, true, true)
                },
                new PropertyDef[] {
                    new PropertyDef("Name", DbTypeCode.String),
                    new PropertyDef("UniqueIdentifierReversed", DbTypeCode.String),
                    new PropertyDef("Inherits", DbTypeCode.Object, "Starcounter.Metadata.Table"),
                    new PropertyDef("Updatable", DbTypeCode.Boolean),
                    new PropertyDef("UniqueIdentifier", DbTypeCode.String),
                    new PropertyDef("FullName", DbTypeCode.String),
                    new PropertyDef("MaterializedTable", DbTypeCode.Object, "Starcounter.Internal.Metadata.MaterializedTable")
                });
        }

        public VMView(Uninitialized u) : base(u) { }
    }

    public sealed class ClrClass : VMView {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_UniqueIdentifierReversed;
            internal static int columnHandle_Inherits;
            internal static int columnHandle_Updatable;
            internal static int columnHandle_UniqueIdentifier;
            internal static int columnHandle_FullName;
            internal static int columnHandle_MaterializedTable;
            internal static int columnHandle_AssemblyName;
            internal static int columnHandle_AppDomainName;
            internal static int columnHandle_FullClassName;
        }
#pragma warning disable 0628, 0169
        #endregion

        static new internal TypeDef CreateTypeDef() {
            System.Type thisSysType = MethodBase.GetCurrentMethod().DeclaringType;
            TypeDef typeDef = TypeDef.CreateTypeTableDef(
                thisSysType,
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("UniqueIdentifierReversed", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("Inherits", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("Updatable", sccoredb.STAR_TYPE_ULONG, false, true),
                    new ColumnDef("UniqueIdentifier", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("FullName", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("MaterializedTable", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("AssemblyName", sccoredb.STAR_TYPE_STRING, true, false),
                    new ColumnDef("AppDomainName", sccoredb.STAR_TYPE_STRING, true, false),
                    new ColumnDef("FullClassName", sccoredb.STAR_TYPE_STRING, true, false)
                },
                new PropertyDef[] {
                    new PropertyDef("Name", DbTypeCode.String),
                    new PropertyDef("UniqueIdentifierReversed", DbTypeCode.String),
                    new PropertyDef("Inherits", DbTypeCode.Object, "Starcounter.Metadata.Table"),
                    new PropertyDef("Updatable", DbTypeCode.Boolean),
                    new PropertyDef("UniqueIdentifier", DbTypeCode.String),
                    new PropertyDef("FullName", DbTypeCode.String),
                    new PropertyDef("MaterializedTable", DbTypeCode.Object, "Starcounter.Internal.Metadata.MaterializedTable"),
                    new PropertyDef("AssemblyName", DbTypeCode.String),
                    new PropertyDef("AppDomainName", DbTypeCode.String),
                    new PropertyDef("FullClassName", DbTypeCode.String)
                });
            return typeDef;
        }

        public ClrClass(Uninitialized u) : base(u) { }
        
        internal ClrClass()
            : this(null) {
            DbState.SystemInsert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }

        public String AssemblyName {
            get {
                return DbState.ReadString(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_AssemblyName);
            }
            internal set {
                DbState.WriteString(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_AssemblyName, value);
            }
        }

        public String AppDomainName {
            get {
                return DbState.ReadString(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_AppDomainName);
            }
            internal set {
                DbState.WriteString(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_AppDomainName, value);
            }
        }

        public String FullClassName {
            get {
                return DbState.ReadString(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_FullClassName);
            }
            internal set {
                DbState.WriteString(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_FullClassName, value);
            }
        }
    }
}

namespace Starcounter.Internal.Metadata {
    public abstract class HostMaterializedTable : Starcounter.Metadata.Table {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_UniqueIdentifierReversed;
            internal static int columnHandle_Inherits;
            internal static int columnHandle_Updatable;
            internal static int columnHandle_UniqueIdentifier;
            internal static int columnHandle_FullName;
            internal static int columnHandle_MaterializedTable;
        }
#pragma warning disable 0628, 0169
        #endregion

        static new internal TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("UniqueIdentifierReversed", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("Inherits", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("Updatable", sccoredb.STAR_TYPE_ULONG, false, true),
                    new ColumnDef("UniqueIdentifier", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("FullName", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("MaterializedTable", sccoredb.STAR_TYPE_REFERENCE, true, false)
                },
                new PropertyDef[] {
                    new PropertyDef("Name", DbTypeCode.String),
                    new PropertyDef("UniqueIdentifierReversed", DbTypeCode.String),
                    new PropertyDef("Inherits", DbTypeCode.Object, "Starcounter.Metadata.Table"),
                    new PropertyDef("Updatable", DbTypeCode.Boolean),
                    new PropertyDef("UniqueIdentifier", DbTypeCode.String),
                    new PropertyDef("FullName", DbTypeCode.String),
                    new PropertyDef("MaterializedTable", DbTypeCode.Object, "Starcounter.Internal.Metadata.MaterializedTable")
                });
        }

        public HostMaterializedTable(Uninitialized u) : base(u) { }

        //internal HostMaterializedTable()
        //    : this(null) {
        //        DbState.Insert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        //}

        public MaterializedTable MaterializedTable {
            get {
                return (MaterializedTable)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                __starcounterTypeSpecification.columnHandle_MaterializedTable);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_MaterializedTable, value);
            }
        }
    }


}