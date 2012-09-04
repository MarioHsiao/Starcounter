
using System;

namespace Starcounter.Binding
{

    public abstract class Int32PropertyBinding : IntPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Int32; } }

        protected override sealed SByte? DoGetSByte(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 32-bit integer value to a 8-bit integer value."
            );
        }

        protected override sealed Int16? DoGetInt16(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 32-bit integer value to a 16-bit integer value."
            );
        }

        protected override sealed Int64? DoGetInt64(object obj)
        {
            return DoGetInt32(obj);
        }
    };
}
