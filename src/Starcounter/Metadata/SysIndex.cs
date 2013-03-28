// ***********************************************************************
// <copyright file="SysIndex.cs" company="Starcounter AB">
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
    /// Class SysIndex
    /// </summary>
    public sealed class SysIndex : Entity
    {
        /// <summary>
        /// Creates the database binding <see cref="TypeDef"/> representing
        /// the type in the database and holding its table- and column defintions.
        /// </summary>
        /// <returns>A <see cref="TypeDef"/> representing the current
        /// type.</returns>
        static internal TypeDef CreateTypeDef() {
            var systemTableDef = new TableDef(
                "sys_index",
                new ColumnDef[]
                {
                    new ColumnDef("index_id", DbTypeCode.UInt64, false, false),
                    new ColumnDef("table_id", DbTypeCode.UInt64, false, false),
                    new ColumnDef("name", DbTypeCode.String, true, false),
                    new ColumnDef("table_name", DbTypeCode.String, true, false),
                    new ColumnDef("description", DbTypeCode.String, true, false),
                    new ColumnDef("unique", DbTypeCode.Boolean, false, false),
                }
                );

            var sysIndexTypeDef = new TypeDef(
                "Starcounter.Metadata.SysIndex",
                null,
                new PropertyDef[]
                {
                    new PropertyDef("TableId", DbTypeCode.UInt64, false) { ColumnName = "table_id" },
                    new PropertyDef("Name", DbTypeCode.String, true) { ColumnName = "name" },
                    new PropertyDef("TableName", DbTypeCode.String, true) { ColumnName = "table_name" },
                    new PropertyDef("Description", DbTypeCode.String, true) { ColumnName = "description" },
                    new PropertyDef("Unique", DbTypeCode.Boolean, false) { ColumnName = "unique" },
                },
                new TypeLoader(new AssemblyName("Starcounter"), "Starcounter.Metadata.SysIndex"),
                systemTableDef
                );

            return sysIndexTypeDef;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SysIndex" /> class.
        /// </summary>
        /// <param name="u">The u.</param>
        public SysIndex(Uninitialized u) : base(u) { }

        /// <summary>
        /// </summary>
        public ulong TableId {
            get { return DbState.ReadUInt64(0,0, 1); }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return DbState.ReadString(0,0, 2); }
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>The name of the table.</value>
        public string TableName
        {
            get { return DbState.ReadString(0,0, 3); }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return DbState.ReadString(0,0, 4); }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="SysIndex" /> is unique.
        /// </summary>
        /// <value><c>true</c> if unique; otherwise, <c>false</c>.</value>
        public bool Unique
        {
            get { return DbState.ReadBoolean(0,0, 5); }
        }
    }
}
