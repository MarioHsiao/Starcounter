﻿// ***********************************************************************
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
    public sealed class SysColumn : Entity {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal sealed class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_table_id = 1;
            internal static int columnHandle_table = 2;
            internal static int columnHandle_index = 3;
            internal static int columnHandle_name = 4;
            internal static int columnHandle_base_type = 5;
            internal static int columnHandle_always_unique = 6;
            internal static int columnHandle_nullable = 7;
            internal static int columnHandle_inherited = 8;
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
                "materialized_column",
                new ColumnDef[]
                {
                    new ColumnDef("__id", DbTypeCode.Key, false, false),
                    new ColumnDef("table_id", DbTypeCode.UInt64, false, false),
                    new ColumnDef("table", DbTypeCode.Object, true, false),
                    new ColumnDef("index", DbTypeCode.UInt64, false, false),
                    new ColumnDef("name", DbTypeCode.String, true, false),
                    new ColumnDef("base_type", DbTypeCode.UInt64, false, false),
                    new ColumnDef("always_unique", DbTypeCode.Boolean, false, false),
                    new ColumnDef("nullable", DbTypeCode.Boolean, false, false),
                    new ColumnDef("inherited", DbTypeCode.Boolean, false, false),
                }
                );

            var sysColumnTypeDef = new TypeDef(
                "Starcounter.Metadata.SysColumn",
                null,
                new PropertyDef[]
                {
                    new PropertyDef("TableId", DbTypeCode.UInt64, false) { ColumnName = "table_id" },
                    new PropertyDef("Table", DbTypeCode.Object, true, "Starcounter.Metadata.SysTable") { ColumnName = "table" },
                    new PropertyDef("Index", DbTypeCode.UInt64, false) { ColumnName = "index" },
                    new PropertyDef("Name", DbTypeCode.String, true) { ColumnName = "name" },
                    new PropertyDef("BaseType", DbTypeCode.UInt64, false) { ColumnName = "base_type" },
                    new PropertyDef("AlwaysUnique", DbTypeCode.Boolean, false) { ColumnName = "always_unique" },
                    new PropertyDef("Nullable", DbTypeCode.Boolean, false) { ColumnName = "nullable" },
                    new PropertyDef("Inherited", DbTypeCode.Boolean, false) { ColumnName = "inherited" },
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
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_table_id); }
        }

        /// <summary>
        /// </summary>
        public SysTable Table {
            get { return (SysTable)DbState.ReadObject(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_table); }
        }

        /// <summary>
        /// </summary>
        public ulong Index {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_index); }
        }

        /// <summary>
        /// </summary>
        public string Name {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_name); }
        }

        /// <summary>
        /// </summary>
        public ulong BaseType {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_base_type); }
        }

        /// <summary>
        /// </summary>
        public bool AlwaysUnique {
            get { return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_always_unique); }
        }

        /// <summary>
        /// </summary>
        public bool Nullable {
            get { return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_nullable); }
        }

        /// <summary>
        /// </summary>
        public bool Inherited {
            get { return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_inherited); }
        }
    }
}
