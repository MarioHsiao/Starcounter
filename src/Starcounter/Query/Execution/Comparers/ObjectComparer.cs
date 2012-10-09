
using Starcounter;
using System;
using System.Collections.Generic;
using Starcounter.Binding;

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
                if (!(value1 is Entity && value2 is Entity))
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Cannot compare non-entity objects.");
                }
                if ((value1 as Entity).ThisRef.ObjectID < (value2 as Entity).ThisRef.ObjectID)
                {
                    return -1;
                }
                if ((value1 as Entity).ThisRef.ObjectID > (value2 as Entity).ThisRef.ObjectID)
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
                if (!(value1 is Entity && value2 is Entity))
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Cannot compare non-entity objects.");
                }
                if ((value1 as Entity).ThisRef.ObjectID < (value2 as Entity).ThisRef.ObjectID)
                {
                    return 1;
                }
                if ((value1 as Entity).ThisRef.ObjectID > (value2 as Entity).ThisRef.ObjectID)
                {
                    return -1;
                }
                return 0;
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect ordering: " + ordering);
        }
    }

    public Int32 Compare(CompositeObject obj1, CompositeObject obj2)
    {
        IObjectView value1 = expression.EvaluateToObject(obj1);
        IObjectView value2 = expression.EvaluateToObject(obj2);
        return InternalCompare(value1, value2);
    }

    public Int32 Compare(ILiteral value, CompositeObject obj)
    {
        if (!(value is ObjectLiteral))
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of value.");
        }
        IObjectView value1 = (value as ObjectLiteral).EvaluateToObject(null);
        IObjectView value2 = expression.EvaluateToObject(obj);
        return InternalCompare(value1, value2);
    }

    public ILiteral Evaluate(CompositeObject obj)
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
}
}
