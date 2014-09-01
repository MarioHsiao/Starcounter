// ***********************************************************************
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
    public abstract class Type : Entity {
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
            System.Type thisSysType = MethodBase.GetCurrentMethod().DeclaringType;
            TypeDef typeDef = TypeDef.CreateTypeTableDef(thisSysType);
            return typeDef;
        }


        /// <inheritdoc />
        public Type(Uninitialized u)
            : base(u) {
        }

        //internal Type()
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

    public sealed class DbPrimitiveType : Starcounter.Metadata.Type {
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
            System.Type thisSysType = MethodBase.GetCurrentMethod().DeclaringType;
            TypeDef typeDef = TypeDef.CreateTypeTableDef(thisSysType);
            return typeDef;
        }


        /// <inheritdoc />
        public DbPrimitiveType(Uninitialized u)
            : base(u) {
        }

        internal DbPrimitiveType()
            : this(null) {
            DbState.Insert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }

        public UInt64 PrimitiveType {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_PrimitiveType); }
            internal set {
                DbState.WriteUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_PrimitiveType,
                    value);
            }
        }
    }

    public abstract class MapPrimitiveType : Starcounter.Metadata.Type {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_DbPrimitiveType;
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
            System.Type thisSysType = MethodBase.GetCurrentMethod().DeclaringType;
            TypeDef typeDef = TypeDef.CreateTypeTableDef(thisSysType);
            return typeDef;
        }

        /// <inheritdoc />
        public MapPrimitiveType(Uninitialized u)
            : base(u) {
        }

        //internal MapPrimitiveType()
        //    : this(null) {
        //    DbState.Insert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        //}

        public DbPrimitiveType DbPrimitiveType {
            get {
                return (DbPrimitiveType)DbState.ReadObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_DbPrimitiveType);
            }
            internal set {
                DbState.WriteObject(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle_DbPrimitiveType, value);
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

    public sealed class ClrPrimitiveType : Starcounter.Metadata.MapPrimitiveType {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal new class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_DbPrimitiveType;
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
            return TypeDef.CreateTypeTableDef(MethodBase.GetCurrentMethod().DeclaringType);
        }

        /// <inheritdoc />
        public ClrPrimitiveType(Uninitialized u)
            : base(u) {
        }

        internal ClrPrimitiveType()
            : this(null) {
            DbState.Insert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }
    }
}
