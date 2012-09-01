
using Starcounter;
using Starcounter.Query;
using System;
using Sc.Server.Internal;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Abstract base class for range points.
/// </summary>
internal abstract class RangePoint
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
}
}
