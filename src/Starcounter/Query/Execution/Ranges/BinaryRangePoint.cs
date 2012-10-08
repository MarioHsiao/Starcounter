
using Starcounter;
using Starcounter.Query;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class BinaryRangePoint : RangePoint, IQueryObject
{
    IBinaryExpression expr;
    BinaryRangeValue cachedRangeValue;

    internal BinaryRangePoint(ComparisonOperator compOp, IBinaryExpression expr)
    {
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        this.compOp = compOp;
        this.expr = expr;
        cachedRangeValue = new BinaryRangeValue();
    }

    internal BinaryRangeValue Evaluate1(IObjectView obj)
    {
        if (expr == null)
        {
            cachedRangeValue.ResetValueToMin(compOp);
        }
        else
        {
            cachedRangeValue.SetValue(compOp, expr.EvaluateToBinary(obj));
        }
        return cachedRangeValue;
    }

    public BinaryRangePoint Clone(VariableArray varArray)
    {
        return new BinaryRangePoint(compOp, expr.CloneToBinary(varArray));
    }

    public IBinaryExpression Expression
    {
        get
        {
            return expr;
        }
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "BinaryRangePoint(");
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
