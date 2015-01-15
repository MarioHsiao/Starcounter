﻿using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter.Metadata {
    public abstract class Member : SystemEntity {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Table;
            internal static int columnHandle_Name;
            internal static int columnHandle_Type;
            internal static int columnHandle_Inherited;
        }
#pragma warning disable 0628, 0169
        #endregion
    
        static internal TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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

        public Boolean Inherited {
            get {
                return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Inherited);
            }
            internal set {
                DbState.WriteBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Inherited, value);
            }
        }
    }

    public sealed class Column : Member {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_MaterializedColumn;
            internal static int columnHandle_Unique;
        }
#pragma warning disable 0628, 0169
        #endregion

        static internal new TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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

    public abstract class Property : Member {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Get;
            internal static int columnHandle_Set;
        }
#pragma warning disable 0628, 0169
        #endregion

        static internal new TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public Property(Uninitialized u) : base(u) { }

        public Boolean Get {
            get {
                return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Get);
            }
            internal set {
                DbState.WriteBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Get, value);
            }
        }
        
        public Boolean Set {
            get {
                return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Set);
            }
            internal set {
                DbState.WriteBoolean(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_Set, value);
            }
        }
    }
    
    public sealed class CodeProperty : Property {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Get;
            internal static int columnHandle_Set;
            internal static int columnHandle_Polymorphic;
        }
#pragma warning disable 0628, 0169
        #endregion

        static internal new TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
