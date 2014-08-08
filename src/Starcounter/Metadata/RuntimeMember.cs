using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter.Metadata {
    public abstract class Member : Entity {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Table;
            internal static int columnHandle_Name;
            internal static int columnHandle_Type;
        }
#pragma warning disable 0628, 0169
        #endregion
    
        static internal TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, false),
                    new ColumnDef("Table", sccoredb.STAR_TYPE_REFERENCE, true, false),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, false),
                    new ColumnDef("Type", sccoredb.STAR_TYPE_REFERENCE, true, false)
                },
                new PropertyDef[] {
                    new PropertyDef("Table", DbTypeCode.Object, "Starcounter.Metadata.Table"),
                    new PropertyDef("Name", DbTypeCode.String),
                    new PropertyDef("Type", DbTypeCode.Object, "Starcounter.Metadata.Type")
                });
        }

        public Member(Uninitialized u) : base(u) { }

        public Table Table {
            get {
                return (Table)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Table);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Table, value);
            }
        }

        public String Name {
            get {
                return DbState.ReadString(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Name);
            }
            internal set {
                DbState.WriteString(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Name, value);
            }
        }

        public Starcounter.Metadata.Type Type {
            get {
                return (Starcounter.Metadata.Type)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Type);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Type, value);
            }
        }
    }

    public sealed class Column : Member {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Table;
            internal static int columnHandle_Name;
            internal static int columnHandle_Type;
            internal static int columnHandle_MaterializedColumn;
            internal static int columnHandle_Unique;
        }
#pragma warning disable 0628, 0169
        #endregion

        static internal new TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Table", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("Type", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("MaterializedColumn", sccoredb.STAR_TYPE_REFERENCE, true, false),
                    new ColumnDef("Unique", sccoredb.STAR_TYPE_ULONG, false, false)
                },
                new PropertyDef[] {
                    new PropertyDef("Table", DbTypeCode.Object, "Starcounter.Metadata.Table"),
                    new PropertyDef("Name", DbTypeCode.String),
                    new PropertyDef("Type", DbTypeCode.Object, "Starcounter.Metadata.Type"),
                    new PropertyDef("MaterializedColumn", DbTypeCode.Object, "Starcounter.Internal.Metadata.MaterializedColumn"),
                    new PropertyDef("Unique", DbTypeCode.Boolean)
                });
        }

        public Column(Uninitialized u) : base(u) { }

        internal Column()
            : this(null) {
                DbState.SystemInsert(__starcounterTypeSpecification.tableHandle, 
                    ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }

        public Starcounter.Internal.Metadata.MaterializedColumn MaterializedColumn {
            get {
                return (Starcounter.Internal.Metadata.MaterializedColumn)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_MaterializedColumn);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_MaterializedColumn, value);
            }
        }

        public Boolean Unique {
            get {
                return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Unique);
            }
            internal set {
                DbState.WriteBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Unique, value);
            }
        }
    }

    public sealed class CodeProperty : Member {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Table;
            internal static int columnHandle_Name;
            internal static int columnHandle_Type;
            internal static int columnHandle_Polymorphic;
        }
#pragma warning disable 0628, 0169
        #endregion

        static internal new TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Table", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("Type", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("Polymorphic", sccoredb.STAR_TYPE_ULONG, false, false)
                },
                new PropertyDef[] {
                    new PropertyDef("Table", DbTypeCode.Object, "Starcounter.Metadata.Table"),
                    new PropertyDef("Name", DbTypeCode.String),
                    new PropertyDef("Type", DbTypeCode.Object, "Starcounter.Metadata.Type"),
                    new PropertyDef("Polymorphic", DbTypeCode.Byte)
                });
        }

        public CodeProperty(Uninitialized u) : base(u) { }

        internal CodeProperty()
            : this(null) {
                DbState.SystemInsert(__starcounterTypeSpecification.tableHandle, 
                    ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }

        public Byte Polymorphic {
            get {
                return DbState.ReadByte(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Polymorphic);
            }
            internal set {
                DbState.WriteByte(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Polymorphic, value);
            }
        }
    }
}
