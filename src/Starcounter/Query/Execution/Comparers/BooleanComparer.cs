
using Starcounter;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a Boolean comparer which is a comparer that
/// uses a Boolean expression and a sort ordering to compare composite objects.
/// </summary>
internal class BooleanComparer : ISingleComparer
{
    protected IBooleanExpression expression;
    protected SortOrder ordering;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logExpr">The expression to be used in the comparison.</param>
    /// <param name="ord">The sort ordering to be used in the comparison.</param>
    internal BooleanComparer(IBooleanExpression expr, SortOrder ord)
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
            return DbTypeCode.Boolean;
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

    private Int32 InternalCompare(Nullable<Boolean> value1, Nullable<Boolean> value2)
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
        Nullable<Boolean> value1 = expression.EvaluateToBoolean(obj1);
        Nullable<Boolean> value2 = expression.EvaluateToBoolean(obj2);
        return InternalCompare(value1, value2);
    }

    public Int32 Compare(ILiteral value, CompositeObject obj)
    {
        if (!(value is BooleanLiteral))
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of value.");
        }
        Nullable<Boolean> value1 = (value as BooleanLiteral).EvaluateToBoolean(null);
        Nullable<Boolean> value2 = expression.EvaluateToBoolean(obj);
        return InternalCompare(value1, value2);
    }

    public ILiteral Evaluate(CompositeObject obj)
    {
        return new BooleanLiteral(expression.EvaluateToBoolean(obj));
    }

    public IQueryComparer Clone(VariableArray varArray)
    {
        return new BooleanComparer(expression.CloneToBoolean(varArray), ordering);
    }

    public ISingleComparer CloneToSingleComparer(VariableArray varArray)
    {
        return new BooleanComparer(expression.CloneToBoolean(varArray), ordering);
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "BooleanComparer(");
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
