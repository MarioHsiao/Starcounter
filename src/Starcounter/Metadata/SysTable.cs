// ***********************************************************************
// <copyright file="SysTable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using System.Reflection;

namespace Starcounter.Metadata {

    /// <summary>
    /// Class SysTable
    /// </summary>
    [Database]
    public sealed class SysTable : Entity {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal sealed class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_table_id = 0;
            internal static int columnHandle_name = 1;
            internal static int columnHandle_base_name = 2;
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
                "sys_table",
                new ColumnDef[]
                {
                    new ColumnDef("__id", DbTypeCode.Key, false, false),
                    new ColumnDef("table_id", DbTypeCode.UInt64, false, false),
                    new ColumnDef("name", DbTypeCode.String, true, false),
                    new ColumnDef("base_name", DbTypeCode.String, true, false),
                }
                );

            var sysTableTypeDef = new TypeDef(
                "Starcounter.Metadata.SysTable",
                null,
                new PropertyDef[]
                {
                    new PropertyDef("TableId", DbTypeCode.UInt64, false) { ColumnName = "table_id" },
                    new PropertyDef("Name", DbTypeCode.String, true) { ColumnName = "name" },
                    new PropertyDef("BaseName", DbTypeCode.String, true) { ColumnName = "base_name" }
                },
                new TypeLoader(new AssemblyName("Starcounter"), "Starcounter.Metadata.SysTable"),
                systemTableDef
                );

            return sysTableTypeDef;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SysTable" /> class.
        /// </summary>
        /// <param name="u">The u.</param>
        public SysTable(Uninitialized u) : base(u) {
        }

        /// <summary>
        /// </summary>
        public ulong TableId {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_table_id); }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_name); }
        }

        /// <summary>
        /// Gets the name of the base.
        /// </summary>
        /// <value>The name of the base.</value>
        public string BaseName {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_base_name); }
        }
    }
}