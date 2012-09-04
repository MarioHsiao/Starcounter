
using System;

namespace Starcounter.Binding
{

    public abstract class SinglePropertyBinding : RealPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Single; } }

        protected override sealed Double DoGetDouble(object obj, out Boolean isNull)
        {
            return DoGetSingle(obj, out isNull);
        }
    }
}
