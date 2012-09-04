
using System;

namespace Starcounter.Binding
{

    public abstract class DoublePropertyBinding : RealPropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Double; } }

        protected override sealed Single? DoGetSingle(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a single to a double."
            );
        }
    }
}
