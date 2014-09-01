using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter.Metadata {
    public class Index : Entity {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_Table;
            internal static int columnHandle_Unique;
            internal static int columnHandle_MaterializedIndex;
        }
#pragma warning disable 0628, 0169
        #endregion

        static internal TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public Index(Uninitialized u) : base(u) { }

        internal Index()
            : this(null) {
            DbState.Insert(__starcounterTypeSpecification.tableHandle,
                ref this.__sc__this_id__, ref this.__sc__this_handle__);
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

        public Starcounter.Internal.Metadata.MaterializedIndex MaterializedIndex {
            get {
                return (Starcounter.Internal.Metadata.MaterializedIndex)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_MaterializedIndex);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_MaterializedIndex, value);
            }
        }
    }

    public class IndexedColumn : Entity {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Index;
            internal static int columnHandle_Column;
            internal static int columnHandle_Position;
            internal static int columnHandle_Ascending;
            internal static int columnHandle_MaterializedIndexColumn;
        }
#pragma warning disable 0628, 0169
        #endregion

        static internal TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public IndexedColumn(Uninitialized u) : base(u) { }

        internal IndexedColumn()
            : this(null) {
            DbState.Insert(__starcounterTypeSpecification.tableHandle,
                ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }

        public Index Index {
            get {
                return (Index)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Index);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Index, value);
            }
        }

        public Column Column {
            get {
                return (Column)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Column);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Column, value);
            }
        }

        public ulong Position {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, 
                __starcounterTypeSpecification.columnHandle_Position); }
            internal set {
                DbState.WriteUInt64(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Position, value);
            }
        }

        public Boolean Ascending {
            get {
                return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Ascending);
            }
            internal set {
                DbState.WriteBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Ascending, value);
            }
        }

        public Starcounter.Internal.Metadata.MaterializedIndexColumn MaterializedIndexColumn {
            get {
                return (Starcounter.Internal.Metadata.MaterializedIndexColumn)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_MaterializedIndexColumn);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_MaterializedIndexColumn, value);
            }
        }
    }
}
