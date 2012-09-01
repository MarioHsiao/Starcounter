
using Starcounter;
using Starcounter.Query;
using System;
using Sc.Server.Internal;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class BooleanRangePoint : RangePoint, IQueryObject
{
    IBooleanExpression expr;
    BooleanRangeValue cachedRangeValue;

    internal BooleanRangePoint(ComparisonOperator compOp, IBooleanExpression expr)
    {
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        this.compOp = compOp;
        this.expr = expr;
        cachedRangeValue = new BooleanRangeValue();
    }

    internal BooleanRangeValue Evaluate1(IObjectView obj)
    {
        if (expr == null)
        {
            cachedRangeValue.ResetValueToMin(compOp);
        }
        else
        {
            cachedRangeValue.SetValue(compOp, expr.EvaluateToBoolean(obj));
        }
        return cachedRangeValue;
    }

    public IBooleanExpression Expression
    {
        get
        {
            return expr;
        }
    }

    public BooleanRangePoint Clone(VariableArray varArray)
    {
        return new BooleanRangePoint(compOp, expr.CloneToBoolean(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "BooleanRangePoint(");
        stringBuilder.AppendLine(tabs + 1, compOp.ToString());
        ;
        expr.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expr.GenerateCompilableCode(stringGen);
    }
}
}
