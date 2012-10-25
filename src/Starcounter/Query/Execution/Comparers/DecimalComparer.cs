// ***********************************************************************
// <copyright file="DecimalComparer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Collections.Generic;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
internal class DecimalComparer : ISingleComparer
{
    protected IDecimalExpression expression;
    protected SortOrder ordering;

    internal DecimalComparer(IDecimalExpression expr, SortOrder ord)
    {
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        ordering = ord;
        expression = expr;
    }

    public DbTypeCode ComparerTypeCode
    {
        get
        {
            return DbTypeCode.Decimal;
        }
    }

    public ITypeExpression Expression
    {
        get
        {
            return expression;
        }
    }

    public SortOrder SortOrdering
    {
        get
        {
            return ordering;
        }
    }

    private Int32 InternalCompare(Nullable<Decimal> value1, Nullable<Decimal> value2)
    {
        switch (ordering)
        {
            case SortOrder.Ascending:
                // Null is regarded as less than any value.
                if (value1 == null && value2 == null)
                {
                    return 0;
                }
                if (value1 == null)
                {
                    return -1;
                }
                if (value2 == null)
                {
                    return 1;
                }
                return value1.Value.CompareTo(value2.Value);
            case SortOrder.Descending:
                // Null is regarded as less than any value.
                if (value1 == null && value2 == null)
                {
                    return 0;
                }
                if (value1 == null)
                {
                    return 1;
                }
                if (value2 == null)
                {
                    return -1;
                }
                return value2.Value.CompareTo(value1.Value);
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect ordering: " + ordering);
        }
    }

    public Int32 Compare(CompositeObject obj1, CompositeObject obj2)
    {
        Nullable<Decimal> value1 = expression.EvaluateToDecimal(obj1);
        Nullable<Decimal> value2 = expression.EvaluateToDecimal(obj2);
        return InternalCompare(value1, value2);
    }

    public Int32 Compare(ILiteral value, CompositeObject obj)
    {
        if (!(value is DecimalLiteral))
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of value.");
        }
        Nullable<Decimal> value1 = (value as DecimalLiteral).EvaluateToDecimal(null);
        Nullable<Decimal> value2 = expression.EvaluateToDecimal(obj);
        return InternalCompare(value1, value2);
    }

    public ILiteral Evaluate(CompositeObject obj)
    {
        return new DecimalLiteral(expression.EvaluateToDecimal(obj));
    }

    public IQueryComparer Clone(VariableArray varArray)
    {
        return new DecimalComparer(expression.CloneToDecimal(varArray), ordering);
    }

    public ISingleComparer CloneToSingleComparer(VariableArray varArray)
    {
        return new DecimalComparer(expression.CloneToDecimal(varArray), ordering);
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "DecimalComparer(");
        expression.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs + 1, ordering.ToString());
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expression.GenerateCompilableCode(stringGen);
    }
}
}
