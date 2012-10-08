
using System;

namespace Starcounter.Binding
{

    public abstract class ObjectPropertyBinding : PropertyBinding
    {

        private TypeBinding targetTypeBinding_;
        private string targetTypeName_;

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Object; } }

        public override sealed ITypeBinding TypeBinding
        {
            get
            {
                TypeBinding tb;
                tb = targetTypeBinding_;
                if (tb != null) return tb;
                return LookupTargetTypeBinding();
            }
        }

        protected override sealed Binary DoGetBinary(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Boolean? DoGetBoolean(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Byte? DoGetByte(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed DateTime? DoGetDateTime(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Decimal? DoGetDecimal(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Double? DoGetDouble(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int16? DoGetInt16(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int32? DoGetInt32(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int64? DoGetInt64(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed SByte? DoGetSByte(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Single? DoGetSingle(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed String DoGetString(object obj)
        {
            Object value;
            value = DoGetObject(obj);
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        protected override sealed UInt16? DoGetUInt16(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed UInt32? DoGetUInt32(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed UInt64? DoGetUInt64(object obj)
        {
            throw ExceptionForInvalidType();
        }

        internal void SetTargetTypeName(string targetTypeName)
        {
            targetTypeName_ = targetTypeName;
        }

        private Exception ExceptionForInvalidType()
        {
            throw new NotSupportedException(
                "Attempt to access a reference attribute as something other then a reference attribute."
            );
        }

        private TypeBinding LookupTargetTypeBinding()
        {
            // Thread-safe because it doesn't matter since the method is
            // idempotent. Field targetTypeBinding_ is only a cache.

            TypeBinding tb = Bindings.GetTypeBinding(targetTypeName_);
            targetTypeBinding_ = tb;
            return tb;
        }
    }
}
