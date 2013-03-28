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
            get { throw new NotImplementedException(); }
        }

        void IObjectProxy.Bind(ulong addr, ulong oid, TypeBinding typeBinding) {
            throw new NotImplementedException();
        }

        ITypeBinding IObjectView.TypeBinding {
            get { throw new NotImplementedException(); }
        }

        bool IObjectView.EqualsOrIsDerivedFrom(IObjectView obj) {
            throw new NotImplementedException();
        }

        Binary? IObjectView.GetBinary(int index) {
            throw new NotImplementedException();
        }

        bool? IObjectView.GetBoolean(int index) {
            throw new NotImplementedException();
        }

        byte? IObjectView.GetByte(int index) {
            throw new NotImplementedException();
        }

        DateTime? IObjectView.GetDateTime(int index) {
            throw new NotImplementedException();
        }

        decimal? IObjectView.GetDecimal(int index) {
            throw new NotImplementedException();
        }

        double? IObjectView.GetDouble(int index) {
            throw new NotImplementedException();
        }

        short? IObjectView.GetInt16(int index) {
            throw new NotImplementedException();
        }

        int? IObjectView.GetInt32(int index) {
            throw new NotImplementedException();
        }

        long? IObjectView.GetInt64(int index) {
            throw new NotImplementedException();
        }

        IObjectView IObjectView.GetObject(int index) {
            throw new NotImplementedException();
        }

        sbyte? IObjectView.GetSByte(int index) {
            throw new NotImplementedException();
        }

        float? IObjectView.GetSingle(int index) {
            throw new NotImplementedException();
        }

        string IObjectView.GetString(int index) {
            throw new NotImplementedException();
        }

        ushort? IObjectView.GetUInt16(int index) {
            throw new NotImplementedException();
        }

        uint? IObjectView.GetUInt32(int index) {
            throw new NotImplementedException();
        }

        ulong? IObjectView.GetUInt64(int index) {
            throw new NotImplementedException();
        }

        bool IObjectView.AssertEquals(IObjectView other) {
            throw new NotImplementedException();
        }

        ulong Advanced.IBindable.Identity {
            get { throw new NotImplementedException(); }
        }
    }
}