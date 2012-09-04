
using System;

namespace Starcounter.Binding
{

    public abstract class BytePropertyBinding : UIntPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Byte; } }

        protected override sealed Int16? DoGetInt16(object obj)
        {
            return DoGetByte(obj);
        }

        protected override sealed Int32? DoGetInt32(object obj)
        {
            return DoGetByte(obj);
        }

        protected override sealed Int64? DoGetInt64(object obj)
        {
            return DoGetByte(obj);
        }

        protected override sealed UInt16? DoGetUInt16(object obj)
        {
            return DoGetByte(obj);
        }

        protected override sealed UInt32? DoGetUInt32(object obj)
        {
            return DoGetByte(obj);
        }

        protected override sealed UInt64? DoGetUInt64(object obj)
        {
            return DoGetByte(obj);
        }
    }
}
