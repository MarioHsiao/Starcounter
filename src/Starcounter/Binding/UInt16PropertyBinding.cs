
using System;

namespace Starcounter.Binding
{

    public abstract class UInt16PropertyBinding : UIntPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.UInt16; } }

        protected override sealed Byte DoGetByte(object obj, out Boolean isNull)
        {
            throw new NotSupportedException(
                "Attempt to convert a 16-bit unsigned value to a 8-bit unsigned value."
            );
        }

        protected override sealed Int16 DoGetInt16(object obj, out Boolean isNull)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int32 DoGetInt32(object obj, out Boolean isNull)
        {
            return DoGetUInt16(obj, out isNull);
        }

        protected override sealed Int64 DoGetInt64(object obj, out Boolean isNull)
        {
            return DoGetUInt16(obj, out isNull);
        }

        protected override sealed UInt32 DoGetUInt32(object obj, out Boolean isNull)
        {
            return DoGetUInt16(obj, out isNull);
        }

        protected override sealed UInt64 DoGetUInt64(object obj, out Boolean isNull)
        {
            return DoGetUInt16(obj, out isNull);
        }
    }
}
