
using System;

namespace Starcounter.Binding
{

    public abstract class UInt64PropertyBinding : UIntPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.UInt64; } }

        protected override sealed Byte? DoGetByte(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 64-bit unsigned value to a 8-bit unsigned value."
            );
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

        protected override sealed UInt16? DoGetUInt16(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 64-bit unsigned value to a 16-bit unsigned value."
            );
        }

        protected override sealed UInt32? DoGetUInt32(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 64-bit unsigned value to a 32-bit unsigned value."
            );
        }
    }
}
