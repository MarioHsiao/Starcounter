
using System;

namespace Starcounter.Binding
{

    public abstract class Int64PropertyBinding : IntPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Int64; } }

        protected override sealed SByte? DoGetSByte(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 64-bit integer value to a 8-bit integer value."
            );
        }

        protected override sealed Int16? DoGetInt16(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 64-bit integer value to a 16-bit integer value."
            );
        }

        protected override sealed Int32? DoGetInt32(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 64-bit integer value to a 32-bit integer value."
            );
        }
    };
}
