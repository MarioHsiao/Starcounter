// ***********************************************************************
// <copyright file="Entity.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using Starcounter.Binding;
using System;

namespace Starcounter.Internal {

    /// <summary>
    /// Serves as a base class for Starcounter-provided entities,
    /// such as system tables.
    /// </summary>
    /// <remarks>
    /// Eventually, we'll rename this class to Entity, once we have
    /// removed all references to the obsolete class with the same
    /// name. Also, we might consider adapting this class so it can
    /// be used by end-user developers too.
    /// </remarks>
    [Database]
    public abstract class Entity : IObjectProxy {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        protected TypeBinding __sc__this_binding__;
        protected ulong __sc__this_handle__;
        protected ulong __sc__this_id__;
#pragma warning disable 0628, 0169
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity" /> class.
        /// </summary>
        /// <param name="u">An instance of <see cref="Uninitialized"/>,
        /// serving the purpose to assure we have a unique signature for
        /// this constructor.</param>
        public Entity(Uninitialized u) : base() {
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            IBindable bindable = obj as IBindable;
            if (bindable == null) {
                return false;
            }
            return (object.ReferenceEquals(this, obj) || (bindable.Identity == this.__sc__this_id__));
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            return this.__sc__this_id__.GetHashCode();
        }

        /// <inheritdoc />
        ulong IObjectProxy.ThisHandle {
            get { return __sc__this_handle__; }
        }

        /// <inheritdoc />
        void IObjectProxy.Bind(ulong addr, ulong oid, TypeBinding typeBinding) {
            __sc__this_handle__ = addr;
            __sc__this_id__ = oid;
            __sc__this_binding__ = typeBinding;
        }

        /// <inheritdoc />
        ITypeBinding IObjectView.TypeBinding {
            get { return __sc__this_binding__; }
        }

        /// <inheritdoc />
        bool IObjectView.EqualsOrIsDerivedFrom(IObjectView obj) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        Binary? IObjectView.GetBinary(int index) {
            return DbState.View.GetBinary(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        bool? IObjectView.GetBoolean(int index) {
            return DbState.View.GetBoolean(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        byte? IObjectView.GetByte(int index) {
            return DbState.View.GetByte(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        DateTime? IObjectView.GetDateTime(int index) {
            return DbState.View.GetDateTime(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        decimal? IObjectView.GetDecimal(int index) {
            return DbState.View.GetDecimal(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        double? IObjectView.GetDouble(int index) {
            return DbState.View.GetDouble(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        short? IObjectView.GetInt16(int index) {
            return DbState.View.GetInt16(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        int? IObjectView.GetInt32(int index) {
            return DbState.View.GetInt32(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        long? IObjectView.GetInt64(int index) {
            return DbState.View.GetInt64(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        IObjectView IObjectView.GetObject(int index) {
            return DbState.View.GetObject(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        sbyte? IObjectView.GetSByte(int index) {
            return DbState.View.GetSByte(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        float? IObjectView.GetSingle(int index) {
            return DbState.View.GetSingle(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        string IObjectView.GetString(int index) {
            return DbState.View.GetString(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        ushort? IObjectView.GetUInt16(int index) {
            return DbState.View.GetUInt16(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        uint? IObjectView.GetUInt32(int index) {
            return DbState.View.GetUInt32(__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        ulong? IObjectView.GetUInt64(int index) {
            return DbState.View.GetUInt64(__sc__this_binding__, index, this);
        }
        
#if DEBUG
        /// <inheritdoc />
        bool IObjectView.AssertEquals(IObjectView other) {
            throw new NotImplementedException();
        }
#endif

        /// <inheritdoc />
        ulong Advanced.IBindable.Identity {
            get { return __sc__this_id__; }
        }
    }
}