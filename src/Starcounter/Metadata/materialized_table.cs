// ***********************************************************************
// <copyright file="materialized_table.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using System.Reflection;

namespace Starcounter.Metadata {

    /// <summary>
    /// Class materialized_table
    /// </summary>
    [Database]
    public sealed class materialized_table : Entity {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal sealed class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_table_id = 1;
            internal static int columnHandle_name = 2;
            internal static int columnHandle_base_table = 3;
        }
#pragma warning disable 0628, 0169
        #endregion

        /// <summary>
        /// Creates the database binding <see cref="TypeDef"/> representing
        /// the type in the database and holding its table- and column defintions.
        /// </summary>
        /// <remarks>
        /// Developer note: if you extend or change this class in any way, make
        /// sure to keep the <see cref="materialized_table.__starcounterTypeSpecification"/>
        /// class in sync with what is returned by this method.
        /// </remarks>
        /// <returns>A <see cref="TypeDef"/> representing the current
        /// type.</returns>
        static internal TypeDef CreateTypeDef() {

            var systemTableDef = new TableDef(
                "materialized_table",
                new ColumnDef[]
                {
                    new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, false),
                    new ColumnDef("table_id", sccoredb.STAR_TYPE_ULONG, false, false),
                    new ColumnDef("name", sccoredb.STAR_TYPE_STRING, true, false),
                    new ColumnDef("base_table", sccoredb.STAR_TYPE_REFERENCE, true, false),
                }
                );

            var sysTableTypeDef = new TypeDef(
                "Starcounter.Metadata.materialized_table",
                null,
                new PropertyDef[]
                {
                    new PropertyDef("table_id", DbTypeCode.UInt64, false) { ColumnName = "table_id" },
                    new PropertyDef("name", DbTypeCode.String, true) { ColumnName = "name" },
                    new PropertyDef("base_table", DbTypeCode.Object, true, "Starcounter.Metadata.materialized_table") { ColumnName = "base_table" }
                },
                new TypeLoader(new AssemblyName("Starcounter"), "Starcounter.Metadata.materialized_table"),
                systemTableDef,
                new DbTypeCode[] {
                    DbTypeCode.Key, DbTypeCode.UInt64, DbTypeCode.String, DbTypeCode.Object
                }
                );

            return sysTableTypeDef;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="materialized_table" /> class.
        /// </summary>
        /// <param name="u">The u.</param>
        public materialized_table(Uninitialized u)
            : base(u) {
        }

        /// <summary>
        /// </summary>
        public ulong table_id {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_table_id); }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string name {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_name); }
        }

        /// <summary>
        /// </summary>
        public materialized_table base_table {
            get { return (materialized_table)DbState.ReadObject(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_base_table); }
        }
    }
}