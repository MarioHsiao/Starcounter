using Starcounter.Advanced;
using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Starcounter {

    public abstract partial class Entity : IBindable, IObjectView, IObjectProxy {
#pragma warning disable 0649, 0169

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected TypeBinding __sc__this_binding__;

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ulong __sc__this_handle__;

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ulong __sc__this_id__;

#pragma warning disable 0628, 0169

        [DebuggerHidden]
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Entity(Uninitialized u) {
        }

        [DebuggerHidden]
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        private Entity(ushort tableId, TypeBinding typeBinding, Initialized dummy) {
            this.__sc__this_binding__ = typeBinding;
            DbState.Insert(tableId, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }

        [DebuggerHidden]
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Entity(ushort tableId, TypeBinding typeBinding, Uninitialized dummy)
            : this(tableId, typeBinding, (Initialized)null) {
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected class __starcounterTypeSpecification {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public static ushort tableHandle;
            [EditorBrowsable(EditorBrowsableState.Never)]
            public static TypeBinding typeBinding;
        }

        /// <inheritdoc />
        void IObjectProxy.Bind(ulong address, ulong oid, TypeBinding typeBinding) {
            this.__sc__this_handle__ = address;
            this.__sc__this_id__ = oid;
            this.__sc__this_binding__ = typeBinding;
        }

#if DEBUG
        /// <inheritdoc />
        bool IObjectView.AssertEquals(IObjectView other) {
            throw new NotImplementedException();
        }
#endif

        /// <inheritdoc />
        bool IObjectView.EqualsOrIsDerivedFrom(IObjectView obj) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        Binary? IObjectView.GetBinary(int index) {
            return DbState.View.GetBinary(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        bool? IObjectView.GetBoolean(int index) {
            return DbState.View.GetBoolean(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        byte? IObjectView.GetByte(int index) {
            return DbState.View.GetByte(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        DateTime? IObjectView.GetDateTime(int index) {
            return DbState.View.GetDateTime(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        decimal? IObjectView.GetDecimal(int index) {
            return DbState.View.GetDecimal(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        double? IObjectView.GetDouble(int index) {
            return DbState.View.GetDouble(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        short? IObjectView.GetInt16(int index) {
            return DbState.View.GetInt16(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        int? IObjectView.GetInt32(int index) {
            return DbState.View.GetInt32(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        long? IObjectView.GetInt64(int index) {
            return DbState.View.GetInt64(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        IObjectView IObjectView.GetObject(int index) {
            return DbState.View.GetObject(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        sbyte? IObjectView.GetSByte(int index) {
            return DbState.View.GetSByte(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        float? IObjectView.GetSingle(int index) {
            return DbState.View.GetSingle(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        string IObjectView.GetString(int index) {
            return DbState.View.GetString(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        ushort? IObjectView.GetUInt16(int index) {
            return DbState.View.GetUInt16(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        uint? IObjectView.GetUInt32(int index) {
            return DbState.View.GetUInt32(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        ulong? IObjectView.GetUInt64(int index) {
            return DbState.View.GetUInt64(this.__sc__this_binding__, index, this);
        }

        /// <inheritdoc />
        ulong IBindable.Identity {
            get {
                return this.__sc__this_id__;
            }
        }

        /// <inheritdoc />
        IBindableRetriever IBindable.Retriever {
            get {
                return DatabaseObjectRetriever.Instance;
            }
        }

        /// <inheritdoc />
        ulong IObjectProxy.ThisHandle {
            get {
                return this.__sc__this_handle__;
            }
        }

        /// <inheritdoc />
        ITypeBinding IObjectView.TypeBinding {
            get {
                return this.__sc__this_binding__;
            }
        }
    }
}
