
using System;

namespace Starcounter.Binding
{

    public abstract class Int16PropertyBinding : IntPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Int16; } }

        protected override sealed SByte DoGetSByte(object obj, out Boolean isNull)
        {
            throw new NotSupportedException(
                "Attempt to convert a 16-bit integer value to a 8-bit integer value."
            );
        }

        protected override sealed Int32 DoGetInt32(object obj, out Boolean isNull)
        {
            return DoGetInt16(obj, out isNull);
        }

        protected override sealed Int64 DoGetInt64(object obj, out Boolean isNull)
        {
            return DoGetInt16(obj, out isNull);
        }
    };
}
