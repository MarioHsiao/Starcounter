
using Starcounter;
using Starcounter.Query;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class StringRangePoint : RangePoint, IQueryObject
{
    IStringExpression expr;
    StringRangeValue cachedRangeValue;
    Boolean appendMaxChar;

    internal StringRangePoint(ComparisonOperator compOp, IStringExpression expr)
    {
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        this.compOp = compOp;
        this.expr = expr;
        appendMaxChar = (expr is StringOperation) && (expr as StringOperation).AppendMaxChar();
        cachedRangeValue = new StringRangeValue();
    }

    internal StringRangeValue Evaluate1(IObjectView obj)
    {
        if (expr == null)
        {
            cachedRangeValue.ResetValueToMin(compOp);
        }
        else
        {
            cachedRangeValue.SetValue(compOp, expr.EvaluateToString(obj), appendMaxChar);
        }
        return cachedRangeValue;
    }

    public IStringExpression Expression
    {
        get
        {
            return expr;
        }
    }

    public StringRangePoint Clone(VariableArray varArray)
    {
        return new StringRangePoint(compOp, expr.CloneToString(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "StringRangePoint(");
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
