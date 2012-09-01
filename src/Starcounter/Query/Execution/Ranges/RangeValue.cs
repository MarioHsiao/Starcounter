
using Starcounter;
using Sc.Server.Internal;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Abstract base class for range values.
/// </summary>
internal abstract class RangeValue
{
    protected ComparisonOperator compOp;

    public ComparisonOperator Operator
    {
        get
        {
            return compOp;
        }
        set
        {
            compOp = value;
        }
    }

    public abstract void ResetValueToMax(ComparisonOperator compOper);
    public abstract void ResetValueToMin(ComparisonOperator compOper);
}
}
