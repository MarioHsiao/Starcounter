
using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Starcounter.Binding;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about an operation with result value of type Double.
/// </summary>
internal class DoubleOperation : IDoubleExpression, INumericalOperation
{
    NumericalOperator numOperator;
    INumericalExpression expr1;
    INumericalExpression expr2;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="numOp">The operator of the operation.</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    internal DoubleOperation(NumericalOperator numOp, INumericalExpression expr1, INumericalExpression expr2) : base()
    {
        if (numOp != NumericalOperator.Addition && numOp != NumericalOperator.Subtraction
            && numOp != NumericalOperator.Multiplication && numOp != NumericalOperator.Division)
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
    internal DoubleOperation(NumericalOperator numOp, INumericalExpression expr)
    : base()
    {
        if (numOp != NumericalOperator.Minus && numOp != NumericalOperator.Plus)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect numOp: " + numOp);
        }
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
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
            return DbTypeCode.Double;
        }
    }
    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.Double;
        }
    }

    /// <summary>
    /// Calculates the value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation when evaluated on the input object.</returns>
    public Nullable<Double> EvaluateToDouble(IObjectView obj)
    {
        Nullable<Double> value1 = expr1.EvaluateToDouble(obj);
        Nullable<Double> value2 = null;
        if (expr2 != null)
        {
            value2 = expr2.EvaluateToDouble(obj);
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
            case NumericalOperator.Division:
                if (value1 == null || value2 == null)
                {
                    return null;
                }
                return value1.Value / value2.Value;
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
        return (Nullable<Decimal>)EvaluateToDouble(obj);
    }

    /// <summary>
    /// Calculates the value of this operation as a nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToInteger(IObjectView obj)
    {
        Nullable<Double> value = EvaluateToDouble(obj);
        if (value == null)
        {
            return null;
        }
        Double roundedValue = Math.Round(value.Value);
        if (roundedValue < (Double)Int64.MinValue)
        {
            return null;
        }
        if (roundedValue > (Double)Int64.MaxValue)
        {
            return null;
        }
        return (Int64)roundedValue;
    }

    /// <summary>
    /// Calculates the value of this operation as a ceiling (round up) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj)
    {
        Nullable<Double> value = EvaluateToDouble(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value < (Double)Int64.MinValue)
        {
            return Int64.MinValue;
        }
        if (value.Value > (Double)Int64.MaxValue)
        {
            return null;
        }
        return (Int64)Math.Ceiling(value.Value);
    }

    /// <summary>
    /// Calculates the value of this operation as a floor (round down) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj)
    {
        Nullable<Double> value = EvaluateToDouble(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value < (Double)Int64.MinValue)
        {
            return null;
        }
        if (value.Value > (Double)Int64.MaxValue)
        {
            return Int64.MaxValue;
        }
        return (Int64)Math.Floor(value.Value);
    }

    /// <summary>
    /// Calculates the value of this operation as a nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUInteger(IObjectView obj)
    {
        Nullable<Double> value = EvaluateToDouble(obj);
        if (value == null)
        {
            return null;
        }
        Double roundedValue = Math.Round(value.Value);
        if (roundedValue < (Double)UInt64.MinValue)
        {
            return null;
        }
        if (roundedValue > (Double)UInt64.MaxValue)
        {
            return null;
        }
        return (UInt64)roundedValue;
    }

    /// <summary>
    /// Calculates the value of this operation as a ceiling (round up) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj)
    {
        Nullable<Double> value = EvaluateToDouble(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value < (Double)UInt64.MinValue)
        {
            return UInt64.MinValue;
        }
        if (value.Value > (Double)UInt64.MaxValue)
        {
            return null;
        }
        return (UInt16)Math.Ceiling(value.Value);
    }

    /// <summary>
    /// Calculates the value of this operation as a floor (round down) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj)
    {
        Nullable<Double> value = EvaluateToDouble(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value < (Double)UInt64.MinValue)
        {
            return null;
        }
        if (value.Value > (Double)UInt64.MaxValue)
        {
            return Int64.MaxValue;
        }
        return (UInt64)Math.Floor(value.Value);
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
        return (EvaluateToDouble(obj) == null);
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
        INumericalExpression instExpr1 = expr1.Instantiate(obj);
        INumericalExpression instExpr2 = null;
        if (expr2 == null)
        {
            instExpr2 = null;
        }
        else
        {
            instExpr2 = expr2.Instantiate(obj);
        }
        if (instExpr2 != null)
        {
            return new DoubleOperation(numOperator, instExpr1, instExpr2);
        }
        return new DoubleOperation(numOperator, instExpr1);
    }

    public ITypeExpression Clone(VariableArray varArray)
    {
        return CloneToDouble(varArray);
    }

    public IDoubleExpression CloneToDouble(VariableArray varArray)
    {
        if (expr2 != null)
        {
            return new DoubleOperation(numOperator, expr1.CloneToNumerical(varArray), expr2.CloneToNumerical(varArray));
        }
        return new DoubleOperation(numOperator, expr1.CloneToNumerical(varArray));
    }

    public INumericalExpression CloneToNumerical(VariableArray varArray)
    {
        if (expr2 != null)
        {
            return new DoubleOperation(numOperator, expr1.CloneToNumerical(varArray), expr2.CloneToNumerical(varArray));
        }
        return new DoubleOperation(numOperator, expr1.CloneToNumerical(varArray));
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
        stringBuilder.AppendLine(tabs, "DoubleOperation(");
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
        throw new NotImplementedException("AppendToInstrAndLeavesList is not implemented for DoubleOperation");
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
