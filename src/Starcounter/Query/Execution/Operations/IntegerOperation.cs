
using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Starcounter.Binding;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about an operation with result value of type integer.
/// </summary>
internal class IntegerOperation : IIntegerExpression, INumericalOperation
{
    NumericalOperator numOperator;
    IIntegerExpression expr1;
    IIntegerExpression expr2;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="numOp">The operator of the operation.</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    internal IntegerOperation(NumericalOperator numOp, IIntegerExpression expr1, IIntegerExpression expr2)
    : base()
    {
        if (numOp != NumericalOperator.Addition && numOp != NumericalOperator.Subtraction
            && numOp != NumericalOperator.Multiplication)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect numOp: " + numOp);
        }
        if (expr1 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
        }
        if (expr2 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr2.");
        }
        numOperator = numOp;
        this.expr1 = expr1;
        this.expr2 = expr2;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="numOp">The operator of the operation.</param>
    /// <param name="logExpr">The operand of the operation.</param>
    internal IntegerOperation(NumericalOperator numOp, IIntegerExpression expr)
    : base()
    {
        if (numOp != NumericalOperator.Minus && numOp != NumericalOperator.Plus)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect numOp: " + numOp);
        }
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        numOperator = numOp;
        expr1 = expr;
        expr2 = null;
    }

    /// <summary>
    /// The DbTypeCode of the result of the operation.
    /// </summary>
    public DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.Int64;
        }
    }
    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.Integer;
        }
    }

    /// <summary>
    /// Calculates the value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation when evaluated on the input object.</returns>
    public Nullable<Int64> EvaluateToInteger(IObjectView obj)
    {
        Nullable<Int64> value1 = expr1.EvaluateToInteger(obj);
        Nullable<Int64> value2 = null;
        if (expr2 != null)
        {
            value2 = expr2.EvaluateToInteger(obj);
        }
        switch (numOperator)
        {
            case NumericalOperator.Addition:
                if (value1 == null || value2 == null)
                {
                    return null;
                }
                return value1.Value + value2.Value;
            case NumericalOperator.Subtraction:
                if (value1 == null || value2 == null)
                {
                    return null;
                }
                return value1.Value - value2.Value;
            case NumericalOperator.Multiplication:
                if (value1 == null || value2 == null)
                {
                    return null;
                }
                return value1.Value * value2.Value;
            case NumericalOperator.Plus:
                if (value1 == null)
                {
                    return null;
                }
                return +value1.Value;
            case NumericalOperator.Minus:
                if (value1 == null)
                {
                    return null;
                }
                return -value1.Value;
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect operator: " + numOperator);
        }
    }

    /// <summary>
    /// Calculates the value of this operation as a nullable Decimal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Decimal> EvaluateToDecimal(IObjectView obj)
    {
        return EvaluateToInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this operation as a nullable Double.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Double> EvaluateToDouble(IObjectView obj)
    {
        return EvaluateToInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this operation as a ceiling (round up) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj)
    {
        return EvaluateToInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this operation as a floor (round down) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj)
    {
        return EvaluateToInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this operation as a nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUInteger(IObjectView obj)
    {
        Nullable<Int64> value = EvaluateToInteger(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value < (Decimal)UInt64.MinValue)
        {
            return null;
        }
        return (UInt64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this operation as a ceiling (round up) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj)
    {
        Nullable<Int64> value = EvaluateToInteger(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value < (Decimal)UInt64.MinValue)
        {
            return UInt64.MinValue;
        }
        return (UInt64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this operation as a floor (round down) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj)
    {
        Nullable<Int64> value = EvaluateToInteger(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value < (Decimal)UInt64.MaxValue)
        {
            return null;
        }
        return (UInt64)value.Value;
    }

    /// <summary>
    /// Examines if the value of the operation is null when evaluated on an input
    /// object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the operation.</param>
    /// <returns>True, if the value of the operation when evaluated on the input
    /// object is null, otherwise false.</returns>
    public Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToInteger(obj) == null);
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public INumericalExpression Instantiate(CompositeObject obj)
    {
        if (expr2 != null)
        {
            return new IntegerOperation(numOperator, (IIntegerExpression)expr1.Instantiate(obj), (IIntegerExpression)expr2.Instantiate(obj));
        }
        return new IntegerOperation(numOperator, (IIntegerExpression)expr1.Instantiate(obj));
    }

    public ITypeExpression Clone(VariableArray varArray)
    {
        return CloneToInteger(varArray);
    }

    public IIntegerExpression CloneToInteger(VariableArray varArray)
    {
        if (expr2 != null)
        {
            return new IntegerOperation(numOperator, expr1.CloneToInteger(varArray), expr2.CloneToInteger(varArray));
        }
        return new IntegerOperation(numOperator, expr1.CloneToInteger(varArray));
    }

    public INumericalExpression CloneToNumerical(VariableArray varArray)
    {
        return CloneToInteger(varArray);
    }

    public void InstantiateExtentSet(ExtentSet extentSet)
    {
        expr1.InstantiateExtentSet(extentSet);
        if (expr2 != null)
        {
            expr2.InstantiateExtentSet(extentSet);
        }
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "IntegerOperation(");
        stringBuilder.AppendLine(tabs + 1, numOperator.ToString());
        expr1.BuildString(stringBuilder, tabs + 1);
        if (expr2 != null)
        {
            expr2.BuildString(stringBuilder, tabs + 1);
        }
        stringBuilder.AppendLine(tabs, ")");
    }

    // No implementation.
    public UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves, CodeGenFilterInstrArray instrArray, Int32 currentExtent, StringBuilder filterText)
    {
        throw new NotImplementedException("AppendToInstrAndLeavesList is not implemented for IntegerOperation");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expr1.GenerateCompilableCode(stringGen);
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, " " + numOperator.ToString() + " ");
        expr2.GenerateCompilableCode(stringGen);
    }
}
}
