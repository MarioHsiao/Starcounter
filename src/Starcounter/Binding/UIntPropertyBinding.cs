
using Starcounter;
using System;

namespace Starcounter.Binding
{

    public abstract class UIntPropertyBinding : PrimitivePropertyBinding
    {

        public UIntPropertyBinding() : base() { }

        protected override sealed Binary DoGetBinary(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Boolean? DoGetBoolean(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed DateTime? DoGetDateTime(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Decimal? DoGetDecimal(object obj)
        {
            UInt64? value;
            value = DoGetUInt64(obj);
            return value.HasValue ? new Decimal?(value.Value) : null;
        }

        protected override sealed Double? DoGetDouble(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Entity DoGetObject(object obj)
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
            UInt64? value;
            value = DoGetUInt64(obj);
            return value.HasValue ? value.Value.ToString() : null;
        }

        internal Exception ExceptionForInvalidType()
        {
            throw new NotSupportedException(
                "Attempt to access an unsigned attribute as something other then an unsigned attribute."
            );
        }
    }
}
