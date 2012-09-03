
using System;

namespace Starcounter.Binding
{

    public abstract class UInt32PropertyBinding : UIntPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.UInt32; } }

        protected override sealed Byte DoGetByte(object obj, out Boolean isNull)
        {
            throw new NotSupportedException(
                "Attempt to convert a 32-bit unsigned value to a 8-bit unsigned value."
            );
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
            return DoGetUInt32(obj, out isNull);
        }

        protected override sealed UInt16 DoGetUInt16(object obj, out Boolean isNull)
        {
            throw new NotSupportedException(
                "Attempt to convert a 32-bit unsigned value to a 16-bit unsigned value."
            );
        }

        protected override sealed UInt64 DoGetUInt64(object obj, out Boolean isNull)
        {
            return DoGetUInt32(obj, out isNull);
        }
    }
}
