// ***********************************************************************
// <copyright file="ObjectRangePoint.cs" company="Starcounter AB">
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
internal class ObjectRangePoint : RangePoint, IQueryObject
{
    IObjectExpression expr;
    ObjectRangeValue cachedRangeValue;

    internal ObjectRangePoint(ComparisonOperator compOp, IObjectExpression expr)
    {
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        this.compOp = compOp;
        this.expr = expr;
        cachedRangeValue = new ObjectRangeValue();
    }

    internal ObjectRangeValue Evaluate1(IObjectView obj)
    {
        if (expr == null)
        {
            cachedRangeValue.ResetValueToMin(compOp);
        }
        else
        {
            cachedRangeValue.SetValue(compOp, expr.EvaluateToObject(obj));
        }
        return cachedRangeValue;
    }

    public IObjectExpression Expression
    {
        get
        {
            return expr;
        }
    }

    public ObjectRangePoint Clone(VariableArray varArray)
    {
        return new ObjectRangePoint(compOp, expr.CloneToObject(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ObjectRangePoint(");
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
