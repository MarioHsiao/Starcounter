using Starcounter.Advanced;
using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Starcounter {

    public abstract partial class Entity2 : IBindable, IObjectView, IObjectProxy {
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
        public Entity2(Uninitialized u) {
        }

        [DebuggerHidden]
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        private Entity2(ushort tableId, TypeBinding typeBinding, Initialized dummy) {
            this.__sc__this_binding__ = typeBinding;
            DbState.Insert(tableId, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }

        [DebuggerHidden]
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Entity2(ushort tableId, TypeBinding typeBinding, Uninitialized dummy)
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

        void IObjectProxy.Bind(ulong address, ulong oid, TypeBinding typeBinding) {
            this.__sc__this_handle__ = address;
            this.__sc__this_id__ = oid;
            this.__sc__this_binding__ = typeBinding;
        }

        bool IObjectView.AssertEquals(IObjectView other) {
            throw new NotImplementedException();
        }

        bool IObjectView.EqualsOrIsDerivedFrom(IObjectView obj) {
            throw new NotImplementedException();
        }

        Binary? IObjectView.GetBinary(int index) {
            return DbState.View.GetBinary(this.__sc__this_binding__, index, this);
        }

        bool? IObjectView.GetBoolean(int index) {
            return DbState.View.GetBoolean(this.__sc__this_binding__, index, this);
        }

        byte? IObjectView.GetByte(int index) {
            return DbState.View.GetByte(this.__sc__this_binding__, index, this);
        }

        DateTime? IObjectView.GetDateTime(int index) {
            return DbState.View.GetDateTime(this.__sc__this_binding__, index, this);
        }

        decimal? IObjectView.GetDecimal(int index) {
            return DbState.View.GetDecimal(this.__sc__this_binding__, index, this);
        }

        double? IObjectView.GetDouble(int index) {
            return DbState.View.GetDouble(this.__sc__this_binding__, index, this);
        }

        short? IObjectView.GetInt16(int index) {
            return DbState.View.GetInt16(this.__sc__this_binding__, index, this);
        }

        int? IObjectView.GetInt32(int index) {
            return DbState.View.GetInt32(this.__sc__this_binding__, index, this);
        }

        long? IObjectView.GetInt64(int index) {
            return DbState.View.GetInt64(this.__sc__this_binding__, index, this);
        }

        IObjectView IObjectView.GetObject(int index) {
            return DbState.View.GetObject(this.__sc__this_binding__, index, this);
        }

        sbyte? IObjectView.GetSByte(int index) {
            return DbState.View.GetSByte(this.__sc__this_binding__, index, this);
        }

        float? IObjectView.GetSingle(int index) {
            return DbState.View.GetSingle(this.__sc__this_binding__, index, this);
        }

        string IObjectView.GetString(int index) {
            return DbState.View.GetString(this.__sc__this_binding__, index, this);
        }

        ushort? IObjectView.GetUInt16(int index) {
            return DbState.View.GetUInt16(this.__sc__this_binding__, index, this);
        }

        uint? IObjectView.GetUInt32(int index) {
            return DbState.View.GetUInt32(this.__sc__this_binding__, index, this);
        }

        ulong? IObjectView.GetUInt64(int index) {
            return DbState.View.GetUInt64(this.__sc__this_binding__, index, this);
        }

        ulong IBindable.Identity {
            get {
                return this.__sc__this_id__;
            }
        }

        IBindableRetriever IBindable.Retriever {
            get {
                return DatabaseObjectRetriever.Instance;
            }
        }

        ulong IObjectProxy.ThisHandle {
            get {
                return this.__sc__this_handle__;
            }
        }

        ITypeBinding IObjectView.TypeBinding {
            get {
                return this.__sc__this_binding__;
            }
        }
    }
}
