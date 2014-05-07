﻿// ***********************************************************************
// <copyright file="BaseType.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using System.Reflection;
using System;
using System.Diagnostics;

namespace Starcounter.Metadata {
    public abstract class BaseType : Entity {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
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
        static internal TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                "Starcounter.Metadata.BaseType", null,
                "BaseType", null,
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, false),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, false)
                },
                new PropertyDef[] {
                    new PropertyDef("Name", DbTypeCode.String)
                });
        }


        /// <inheritdoc />
        public BaseType(Uninitialized u)
            : base(u) {
        }

        //internal BaseType()
        //    : this(null) {
        //    DbState.Insert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        //}

        /// <summary>
        /// Name of the type
        /// </summary>
        public string Name {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Name); }
            internal set {
                DbState.WriteString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Name,
                    value);
            }
        }
    }

    public sealed class MaterializedType : BaseType {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_PrimitiveType;
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
        static internal new TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                "Starcounter.Metadata.MaterializedType", "Starcounter.Metadata.BaseType",
                "MaterializedType", "BaseType",
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("PrimitiveType", sccoredb.STAR_TYPE_ULONG, false, false)
                },
                new PropertyDef[] {
                    new PropertyDef("Name", DbTypeCode.String),
                    new PropertyDef("PrimitiveType", DbTypeCode.UInt64)
                });
        }


        /// <inheritdoc />
        public MaterializedType(Uninitialized u)
            : base(u) {
        }

        internal MaterializedType()
            : this(null) {
            DbState.SystemInsert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }

        public UInt64 PrimitiveType {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_PrimitiveType); }
            internal set {
                DbState.WriteUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_PrimitiveType,
                    value);
            }
        }
    }

    public abstract class MappedType : BaseType {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_MaterializedType;
            internal static int columnHandle_WriteLoss;
            internal static int columnHandle_ReadLoss;
            internal static int columnHandle_DbTypeCode;
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
        static internal new TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                "Starcounter.Metadata.MappedType", "Starcounter.Metadata.BaseType",
                "MappedType", "BaseType",
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("MaterializedType", sccoredb.STAR_TYPE_REFERENCE, true, false),
                    new ColumnDef("WriteLoss", sccoredb.STAR_TYPE_ULONG, false, false),
                    new ColumnDef("ReadLoss", sccoredb.STAR_TYPE_ULONG, false, false),
                    new ColumnDef("DbTypeCode", sccoredb.STAR_TYPE_ULONG, false,false)
                },
                new PropertyDef[] {
                    new PropertyDef("Name", Starcounter.Binding.DbTypeCode.String),
                    new PropertyDef("MaterializedType", Starcounter.Binding.DbTypeCode.Object, "Starcounter.Metadata.MaterializedType"),
                    new PropertyDef("WriteLoss", Starcounter.Binding.DbTypeCode.Boolean),
                    new PropertyDef("ReadLoss",  Starcounter.Binding.DbTypeCode.Boolean),
                    new PropertyDef("DbTypeCode", Starcounter.Binding.DbTypeCode.UInt16)
                });
        }

        /// <inheritdoc />
        public MappedType(Uninitialized u)
            : base(u) {
        }

        //internal MappedType()
        //    : this(null) {
        //    DbState.Insert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        //}

        public MaterializedType MaterializedType {
            get {
                return (MaterializedType)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_MaterializedType);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_MaterializedType, value);
            }
        }

        public Boolean WriteLoss {
            get { return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_WriteLoss); }
            internal set {
                DbState.WriteBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_WriteLoss,
                    value);
            }
        }

        public Boolean ReadLoss {
            get { return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_ReadLoss); }
            internal set {
                DbState.WriteBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_ReadLoss,
                    value);
            }
        }

        public UInt16 DbTypeCode {
            get { return DbState.ReadUInt16(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_DbTypeCode); }
            internal set {
                DbState.WriteUInt16(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_DbTypeCode,
                    value);
            }
        }
    }

    public sealed class ClrPrimitiveType : MappedType {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_MaterializedType;
            internal static int columnHandle_WriteLoss;
            internal static int columnHandle_ReadLoss;
            internal static int columnHandle_DbTypeCode;
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
        static internal new TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(
                "Starcounter.Metadata.ClrPrimitiveType", "Starcounter.Metadata.MappedType",
                "ClrPrimitiveType", "MappedType",
                new ColumnDef[] {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, true),
                    new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, true),
                    new ColumnDef("MaterializedType", sccoredb.STAR_TYPE_REFERENCE, true, true),
                    new ColumnDef("WriteLoss", sccoredb.STAR_TYPE_ULONG, false, true),
                    new ColumnDef("ReadLoss", sccoredb.STAR_TYPE_ULONG, false, true),
                    new ColumnDef("DbTypeCode", sccoredb.STAR_TYPE_ULONG, false,true)
                },
                new PropertyDef[] {
                    new PropertyDef("Name", Starcounter.Binding.DbTypeCode.String),
                    new PropertyDef("MaterializedType", Starcounter.Binding.DbTypeCode.Object, "Starcounter.Metadata.MaterializedType"),
                    new PropertyDef("WriteLoss", Starcounter.Binding.DbTypeCode.Boolean),
                    new PropertyDef("ReadLoss",  Starcounter.Binding.DbTypeCode.Boolean),
                    new PropertyDef("DbTypeCode", Starcounter.Binding.DbTypeCode.UInt16)
                });
        }

        /// <inheritdoc />
        public ClrPrimitiveType(Uninitialized u)
            : base(u) {
        }

        internal ClrPrimitiveType()
            : this(null) {
            DbState.SystemInsert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }
    }
}