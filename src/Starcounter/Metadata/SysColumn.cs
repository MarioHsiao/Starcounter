// ***********************************************************************
// <copyright file="SysTable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Reflection;

namespace Starcounter.Metadata
{
    /// <summary>
    /// </summary>
    public sealed class SysColumn
    {
        public ObjectRef ThisRef;

        /// <summary>
        /// Creates the database binding <see cref="TypeDef"/> representing
        /// the type in the database and holding its table- and column defintions.
        /// </summary>
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

        /// <summary>
        /// </summary>
        public ulong TableId {
            get { return DbState.ReadUInt64(this.ThisRef.ObjectID, ThisRef.ETI, 0); }
        }

        /// <summary>
        /// </summary>
        public ulong Index {
            get { return DbState.ReadUInt64(ThisRef.ObjectID, ThisRef.ETI, 1); }
        }

        /// <summary>
        /// </summary>
        public string Name {
            get { return DbState.ReadString(ThisRef.ObjectID, ThisRef.ETI, 2); }
        }
    }
}
