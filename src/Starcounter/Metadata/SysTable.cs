// ***********************************************************************
// <copyright file="SysTable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Reflection;

namespace Starcounter.Metadata {

    /// <summary>
    /// Class SysTable
    /// </summary>
    [Database]
    public sealed class SysTable : IObjectProxy {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal sealed class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_TableId = 0;
            internal static int columnHandle_Name = 1;
            internal static int columnHandle_BaseName = 2;
        }
        TypeBinding __sc__this_binding__;
        ulong __sc__this_handle__;
        ulong __sc__this_id__;
#pragma warning disable 0628, 0169
        #endregion

        /// <summary>
        /// Creates the database binding <see cref="TypeDef"/> representing
        /// the type in the database and holding its table- and column defintions.
        /// </summary>
        /// <returns>A <see cref="TypeDef"/> representing the current
        /// type.</returns>
        static internal TypeDef CreateTypeDef() {

            var systemTableDef = new TableDef(
                "sys_table",
                new ColumnDef[]
                {
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
        public SysTable(Uninitialized u) : base() {
        }

        /// <summary>
        /// </summary>
        public ulong TableId {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_TableId); }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Name); }
        }

        /// <summary>
        /// Gets the name of the base.
        /// </summary>
        /// <value>The name of the base.</value>
        public string BaseName {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_BaseName); }
        }

        ulong IObjectProxy.ThisHandle {
            get { return __sc__this_handle__; }
        }

        void IObjectProxy.Bind(ulong addr, ulong oid, TypeBinding typeBinding) {
            __sc__this_handle__ = addr;
            __sc__this_id__ = oid;
            __sc__this_binding__ = typeBinding;
        }

        ITypeBinding IObjectView.TypeBinding {
            get { return __sc__this_binding__; }
        }

        bool IObjectView.EqualsOrIsDerivedFrom(IObjectView obj) {
            throw new NotImplementedException();
        }

        Binary? IObjectView.GetBinary(int index) {
            return DbState.View.GetBinary(__sc__this_binding__, index, this);
        }

        bool? IObjectView.GetBoolean(int index) {
            return DbState.View.GetBoolean(__sc__this_binding__, index, this);
        }

        byte? IObjectView.GetByte(int index) {
            return DbState.View.GetByte(__sc__this_binding__, index, this);
        }

        DateTime? IObjectView.GetDateTime(int index) {
            return DbState.View.GetDateTime(__sc__this_binding__, index, this);
        }

        decimal? IObjectView.GetDecimal(int index) {
            return DbState.View.GetDecimal(__sc__this_binding__, index, this);
        }

        double? IObjectView.GetDouble(int index) {
            return DbState.View.GetDouble(__sc__this_binding__, index, this);
        }

        short? IObjectView.GetInt16(int index) {
            return DbState.View.GetInt16(__sc__this_binding__, index, this);
        }

        int? IObjectView.GetInt32(int index) {
            return DbState.View.GetInt32(__sc__this_binding__, index, this);
        }

        long? IObjectView.GetInt64(int index) {
            return DbState.View.GetInt64(__sc__this_binding__, index, this);
        }

        IObjectView IObjectView.GetObject(int index) {
            return DbState.View.GetObject(__sc__this_binding__, index, this);
        }

        sbyte? IObjectView.GetSByte(int index) {
            return DbState.View.GetSByte(__sc__this_binding__, index, this);
        }

        float? IObjectView.GetSingle(int index) {
            return DbState.View.GetSingle(__sc__this_binding__, index, this);
        }

        string IObjectView.GetString(int index) {
            return DbState.View.GetString(__sc__this_binding__, index, this);
        }

        ushort? IObjectView.GetUInt16(int index) {
            return DbState.View.GetUInt16(__sc__this_binding__, index, this);
        }

        uint? IObjectView.GetUInt32(int index) {
            return DbState.View.GetUInt32(__sc__this_binding__, index, this);
        }

        ulong? IObjectView.GetUInt64(int index) {
            return DbState.View.GetUInt64(__sc__this_binding__, index, this);
        }

        bool IObjectView.AssertEquals(IObjectView other) {
            throw new NotImplementedException();
        }

        ulong Advanced.IBindable.Identity {
            get { return __sc__this_id__; }
        }
    }
}