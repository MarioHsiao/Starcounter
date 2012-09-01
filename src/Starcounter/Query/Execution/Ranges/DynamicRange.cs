
using Starcounter;
using Starcounter.Query.Execution;
using Starcounter.Query.Sql;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Collections;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Abstract base class for dynamic ranges.
/// </summary>
internal abstract class DynamicRange
{
    protected ILogicalExpression logExpr; // The logical expression corresponding to the range-point-list.

    public ILogicalExpression LogicalExpression
    {
        get
        {
            if (logExpr != null)
            {
                return logExpr;
            }
            return new LogicalLiteral(TruthValue.TRUE);
        }
    }
}
}
