
using System;

namespace Starcounter.Binding
{

    public abstract class SBytePropertyBinding : IntPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.SByte; } }

        protected override sealed Int16 DoGetInt16(object obj, out Boolean isNull)
        {
            return DoGetSByte(obj, out isNull);
        }

        protected override sealed Int32 DoGetInt32(object obj, out Boolean isNull)
        {
            return DoGetSByte(obj, out isNull);
        }

        protected override sealed Int64 DoGetInt64(object obj, out Boolean isNull)
        {
            return DoGetSByte(obj, out isNull);
        }
    };
}
