// ***********************************************************************
// <copyright file="ObjectComparer.cs" company="Starcounter AB">
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
internal class ObjectComparer : ISingleComparer
{
    protected IObjectExpression expression;
    protected SortOrder ordering;

    internal ObjectComparer(IObjectExpression expr, SortOrder ord)
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
            return DbTypeCode.Object;
        }
    }

    public IValueExpression Expression
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

    private Int32 InternalCompare(IObjectView value1, IObjectView value2)
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
                //if (!(value1 is Entity && value2 is Entity))
                //{
                //    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Cannot compare non-entity objects.");
                //}
                if (value1.Identity < value2.Identity)
                {
                    return -1;
                }
                if (value1.Identity > value2.Identity)
                {
                    return 1;
                }
                return 0;
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
                //if (!(value1 is Entity && value2 is Entity))
                //{
                //    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Cannot compare non-entity objects.");
                //}
                if (value1.Identity < value2.Identity)
                {
                    return 1;
                }
                if (value1.Identity > value2.Identity)
                {
                    return -1;
                }
                return 0;
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect ordering: " + ordering);
        }
    }

    public Int32 Compare(Row obj1, Row obj2)
    {
        IObjectView value1 = expression.EvaluateToObject(obj1);
        IObjectView value2 = expression.EvaluateToObject(obj2);
        return InternalCompare(value1, value2);
    }

    public Int32 Compare(ILiteral value, Row obj)
    {
        if (!(value is ObjectLiteral))
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of value.");
        }
        IObjectView value1 = (value as ObjectLiteral).EvaluateToObject(null);
        IObjectView value2 = expression.EvaluateToObject(obj);
        return InternalCompare(value1, value2);
    }

    public ILiteral Evaluate(Row obj)
    {
        IObjectView value = expression.EvaluateToObject(obj);
        if (value != null)
        {
            return new ObjectLiteral(expression.EvaluateToObject(obj));
        }
        return new ObjectLiteral(expression.TypeBinding);
    }

    public IQueryComparer Clone(VariableArray varArray)
    {
        return new ObjectComparer(expression.CloneToObject(varArray), ordering);
    }

    public ISingleComparer CloneToSingleComparer(VariableArray varArray)
    {
        return new ObjectComparer(expression.CloneToObject(varArray), ordering);
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ObjectComparer(");
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
        ObjectComparer otherNode = other as ObjectComparer;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(ObjectComparer other) {
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
