// ***********************************************************************
// <copyright file="SysColumn.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using System.Reflection;

namespace Starcounter.Metadata {
    
    /// <summary>
    /// </summary>
    public sealed class SysColumn : Entity2 {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal sealed class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_TableId = 0;
            internal static int columnHandle_Index = 1;
            internal static int columnHandle_Name = 2;
        }
#pragma warning disable 0628, 0169
        #endregion

        /// <summary>
        /// Creates the database binding <see cref="TypeDef"/> representing
        /// the type in the database and holding its table- and column defintions.
        /// </summary>
        /// <remarks>
        /// Developer note: if you extend or change this class in any way, make
        /// sure to keep the <see cref="SysTable.__starcounterTypeSpecification"/>
        /// class in sync with what is returned by this method.
        /// </remarks>
        /// <returns>A <see cref="TypeDef"/> representing the current
        /// type.</returns>
        static internal TypeDef CreateTypeDef() {
            var systemTableDef = new TableDef(
                "sys_column",
                new ColumnDef[]
                {
                    new ColumnDef("table_id", DbTypeCode.UInt64, false, false),
                    new ColumnDef("index", DbTypeCode.UInt64, false, false),
                    new ColumnDef("name", DbTypeCode.String, true, false),
                }
                );

            var sysColumnTypeDef = new TypeDef(
                "Starcounter.Metadata.SysColumn",
                null,
                new PropertyDef[]
                {
                    new PropertyDef("TableId", DbTypeCode.UInt64, false) { ColumnName = "table_id" },
                    new PropertyDef("Index", DbTypeCode.UInt64, false) { ColumnName = "index" },
                    new PropertyDef("Name", DbTypeCode.String, true) { ColumnName = "name" },
                },
                new TypeLoader(new AssemblyName("Starcounter"), "Starcounter.Metadata.SysColumn"),
                systemTableDef
                );

            return sysColumnTypeDef;
        }

        /// <inheritdoc />
        public SysColumn(Uninitialized u) : base(u) {
        }

        /// <summary>
        /// </summary>
        public ulong TableId {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_TableId); }
        }

        /// <summary>
        /// </summary>
        public ulong Index {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Index); }
        }

        /// <summary>
        /// </summary>
        public string Name {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Name); }
        }
    }
}
