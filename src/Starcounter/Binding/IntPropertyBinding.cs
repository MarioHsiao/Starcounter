
using System;

namespace Starcounter.Binding
{

    public abstract class IntPropertyBinding : PrimitivePropertyBinding
    {

        public IntPropertyBinding() : base() { }

        protected override sealed Binary DoGetBinary(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Boolean DoGetBoolean(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Byte DoGetByte(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed DateTime DoGetDateTime(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Decimal DoGetDecimal(object obj, out Boolean isNull)
        {
            return new Decimal(DoGetInt64(obj, out isNull));
        }

        protected override sealed Double DoGetDouble(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Entity DoGetObject(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Single DoGetSingle(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed String DoGetString(object obj)
        {
            Int64 value;
            Boolean isNull;
            value = DoGetInt64(obj, out isNull);
            if (isNull)
            {
                return null;
            }
            return value.ToString();
        }

        protected override sealed UInt16 DoGetUInt16(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed UInt32 DoGetUInt32(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed UInt64 DoGetUInt64(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        private Exception ExceptionForInvalidType()
        {
            throw new NotSupportedException(
                "Attempt to access an integer attribute as something other then an integer attribute."
            );
        }
    }
}
