
using Sc.Server.Internal;
using System;
using Starcounter;


namespace Starcounter.Binding
{

    public class LargeBinaryPropertyBinding : PrimitivePropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.LargeBinary; } }

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
            throw ExceptionForInvalidType();
        }

        protected override sealed Double DoGetDouble(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int16 DoGetInt16(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int32 DoGetInt32(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int64 DoGetInt64(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Binary DoGetBinary(object obj)
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
            throw ExceptionForInvalidType();
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
                "Attempt to access a large binary attribute as something other then a large binary attribute."
            );
        }
    }
}
