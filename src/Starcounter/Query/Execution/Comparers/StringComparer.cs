// ***********************************************************************
// <copyright file="StringComparer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Collections.Generic;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
internal class StringComparer : ISingleComparer
{
    protected IStringExpression expression;
    protected SortOrder ordering;

    internal StringComparer(IStringExpression expr, SortOrder ord)
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
            return DbTypeCode.String;
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

    private Int32 InternalCompare(String value1, String value2)
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
                return DbHelper.StringCompare(value1, value2);
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
                return DbHelper.StringCompare(value2, value1);
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect ordering: " + ordering);
        }
    }

    public Int32 Compare(CompositeObject obj1, CompositeObject obj2)
    {
        String value1 = expression.EvaluateToString(obj1);
        String value2 = expression.EvaluateToString(obj2);
        return InternalCompare(value1, value2);
    }

    public Int32 Compare(ILiteral value, CompositeObject obj)
    {
        if (!(value is StringLiteral))
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of value.");
        }
        String value1 = (value as StringLiteral).EvaluateToString(null);
        String value2 = expression.EvaluateToString(obj);
        return InternalCompare(value1, value2);
    }

    public ILiteral Evaluate(CompositeObject obj)
    {
        return new StringLiteral(expression.EvaluateToString(obj));
    }

    public IQueryComparer Clone(VariableArray varArray)
    {
        return new StringComparer(expression.CloneToString(varArray), ordering);
    }

    public ISingleComparer CloneToSingleComparer(VariableArray varArray)
    {
        return new StringComparer(expression.CloneToString(varArray), ordering);
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "StringComparer(");
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

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(ISingleComparer other) {
        StringComparer otherNode = other as StringComparer;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(StringComparer other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check if there are not cyclic references
        Debug.Assert(!this.AssertEqualsVisited);
        if (this.AssertEqualsVisited)
            return false;
        Debug.Assert(!other.AssertEqualsVisited);
        if (other.AssertEqualsVisited)
            return false;
        // Check basic types
        Debug.Assert(this.ordering == other.ordering);
        if (this.ordering != other.ordering)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = this.expression.AssertEquals(other.expression);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
