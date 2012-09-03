
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

        protected override sealed Boolean DoGetBoolean(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed DateTime DoGetDateTime(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Decimal DoGetDecimal(object obj, out Boolean isNull)
        {
            return new Decimal(DoGetUInt64(obj, out isNull));
        }

        protected override sealed Double DoGetDouble(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Entity DoGetObject(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed SByte DoGetSByte(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Single DoGetSingle(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed String DoGetString(object obj)
        {
            UInt64 value;
            Boolean isNull;
            value = DoGetUInt64(obj, out isNull);
            if (isNull)
            {
                return null;
            }
            return value.ToString();
        }

        internal Exception ExceptionForInvalidType()
        {
            throw new NotSupportedException(
                "Attempt to access an unsigned attribute as something other then an unsigned attribute."
            );
        }
    }
}
