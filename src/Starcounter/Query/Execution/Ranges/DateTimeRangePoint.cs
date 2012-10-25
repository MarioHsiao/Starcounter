// ***********************************************************************
// <copyright file="DateTimeRangePoint.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class DateTimeRangePoint : RangePoint, IQueryObject
{
    IDateTimeExpression expr;
    DateTimeRangeValue cachedRangeValue;

    internal DateTimeRangePoint(ComparisonOperator compOp, IDateTimeExpression expr)
    {
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        this.compOp = compOp;
        this.expr = expr;
        cachedRangeValue = new DateTimeRangeValue();
    }

    internal DateTimeRangeValue Evaluate1(IObjectView obj)
    {
        if (expr == null)
        {
            cachedRangeValue.ResetValueToMin(compOp);
        }
        else
        {
            cachedRangeValue.SetValue(compOp, expr.EvaluateToDateTime(obj));
        }
        return cachedRangeValue;
    }

    public IDateTimeExpression Expression
    {
        get
        {
            return expr;
        }
    }

    public DateTimeRangePoint Clone(VariableArray varArray)
    {
        return new DateTimeRangePoint(compOp, expr.CloneToDateTime(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "DateTimeRangePoint(");
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
