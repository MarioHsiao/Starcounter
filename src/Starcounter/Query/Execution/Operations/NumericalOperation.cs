using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a numerical operation with result value of some
/// of the types: integer, unsigned integer, decimal or double.
/// </summary>
internal class NumericalOperation : INumericalExpression, INumericalOperation
{
    NumericalOperator numOperator;
    INumericalExpression expr1;
    INumericalExpression expr2;
    DbTypeCode dbTypeCode;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="numOp">The operator of the operation.</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    internal NumericalOperation(NumericalOperator numOp, INumericalExpression expr1, INumericalExpression expr2) : base()
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

        if (expr1.DbTypeCode == DbTypeCode.Double || expr2.DbTypeCode == DbTypeCode.Double ||
            expr1.DbTypeCode == DbTypeCode.Single || expr2.DbTypeCode == DbTypeCode.Single)
        {
            dbTypeCode = DbTypeCode.Double;
        }
        else
        {
            dbTypeCode = DbTypeCode.Decimal;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="numOp">The operator of the operation.</param>
    /// <param name="logExpr">The operand of the operation.</param>
    internal NumericalOperation(NumericalOperator numOp, INumericalExpression expr)
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
        if (expr1.DbTypeCode == DbTypeCode.Double ||
            expr1.DbTypeCode == DbTypeCode.Single)
        {
            dbTypeCode = DbTypeCode.Double;
        }
        else
        {
            dbTypeCode = DbTypeCode.Decimal;
        }
    }

    /// <summary>
    /// The DbTypeCode of the result of the operation.
    /// </summary>
    public DbTypeCode DbTypeCode
    {
        get
        {
            return dbTypeCode;
        }
    }

    /// <summary>
    /// The QueryTypeCode of the result of the operation.
    /// </summary>
    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.Numerical;
        }
    }

    /// <summary>
    /// Calculates the value of this operation as a nullable Decimal.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Decimal> EvaluateToDecimal(IObjectView obj)
    {
        Nullable<Decimal> value1 = expr1.EvaluateToDecimal(obj);
        Nullable<Decimal> value2 = null;
        if (expr2 != null)
        {
            value2 = expr2.EvaluateToDecimal(obj);
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
    /// Calculates the value of this operation as a nullable Double.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation.</returns>
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
    /// Calculates the value of this operation as a nullable Int64.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToInteger(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Decimal:
                Nullable<Decimal> decValue = EvaluateToDecimal(obj);
                if (decValue == null)
                {
                    return null;
                }
                decValue = Math.Round(decValue.Value);
                if (decValue.Value < (Decimal)Int64.MinValue)
                {
                    return null;
                }
                if (decValue.Value > (Decimal)Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)decValue.Value;

            case DbTypeCode.Double:
                Nullable<Double> dblValue = EvaluateToDouble(obj);
                if (dblValue == null)
                {
                    return null;
                }
                dblValue = Math.Round(dblValue.Value);
                if (dblValue.Value < (Double)Int64.MinValue)
                {
                    return null;
                }
                if (dblValue.Value > (Double)Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)dblValue.Value;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this operation as a ceiling (round up) nullable Int64.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Decimal:
                Nullable<Decimal> decValue = EvaluateToDecimal(obj);
                if (decValue == null)
                {
                    return null;
                }
                if (decValue.Value < (Decimal)Int64.MinValue)
                {
                    return Int64.MinValue;
                }
                if (decValue.Value > (Decimal)Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)Math.Ceiling(decValue.Value);

            case DbTypeCode.Double:
                Nullable<Double> dblValue = EvaluateToDouble(obj);
                if (dblValue == null)
                {
                    return null;
                }
                if (dblValue.Value < (Double)Int64.MinValue)
                {
                    return Int64.MinValue;
                }
                if (dblValue.Value > (Double)Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)Math.Ceiling(dblValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this operation as a floor (round down) nullable Int64.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Decimal:
                Nullable<Decimal> decValue = EvaluateToDecimal(obj);
                if (decValue == null)
                {
                    return null;
                }
                if (decValue.Value < (Decimal)Int64.MinValue)
                {
                    return null;
                }
                if (decValue.Value > (Decimal)Int64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (Int64)Math.Floor(decValue.Value);

            case DbTypeCode.Double:
                Nullable<Double> dblValue = EvaluateToDouble(obj);
                if (dblValue == null)
                {
                    return null;
                }
                if (dblValue.Value < (Double)Int64.MinValue)
                {
                    return null;
                }
                if (dblValue.Value > (Double)Int64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (Int64)Math.Floor(dblValue.Value);
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this operation as a nullable UInt64.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUInteger(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Decimal:
                Nullable<Decimal> decValue = EvaluateToDecimal(obj);
                if (decValue == null)
                {
                    return null;
                }
                decValue = Math.Round(decValue.Value);
                if (decValue.Value < (Decimal)UInt64.MinValue)
                {
                    return null;
                }
                if (decValue.Value > (Decimal)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)decValue.Value;

            case DbTypeCode.Double:
                Nullable<Double> dblValue = EvaluateToDouble(obj);
                if (dblValue == null)
                {
                    return null;
                }
                dblValue = Math.Round(dblValue.Value);
                if (dblValue.Value < (Double)UInt64.MinValue)
                {
                    return null;
                }
                if (dblValue.Value > (Double)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)dblValue.Value;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this operation as a ceiling (round up) nullable UInt64.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Decimal:
                Nullable<Decimal> decValue = EvaluateToDecimal(obj);
                if (decValue == null)
                {
                    return null;
                }
                if (decValue.Value < (Decimal)UInt64.MinValue)
                {
                    return UInt64.MinValue;
                }
                if (decValue.Value > (Decimal)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)Math.Ceiling(decValue.Value);

            case DbTypeCode.Double:
                Nullable<Double> dblValue = EvaluateToDouble(obj);
                if (dblValue == null)
                {
                    return null;
                }
                if (dblValue.Value < (Double)UInt64.MinValue)
                {
                    return UInt64.MinValue;
                }
                if (dblValue.Value > (Double)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)Math.Ceiling(dblValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this operation as a floor (round down) nullable UInt64.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Decimal:
                Nullable<Decimal> decValue = EvaluateToDecimal(obj);
                if (decValue == null)
                {
                    return null;
                }
                if (decValue.Value < (Decimal)UInt64.MinValue)
                {
                    return null;
                }
                if (decValue.Value > (Decimal)UInt64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (UInt64)Math.Floor(decValue.Value);

            case DbTypeCode.Double:
                Nullable<Double> dblValue = EvaluateToDouble(obj);
                if (dblValue == null)
                {
                    return null;
                }
                if (dblValue.Value < (Double)UInt64.MinValue)
                {
                    return null;
                }
                if (dblValue.Value > (Double)UInt64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (UInt64)Math.Floor(dblValue.Value);
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
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
        switch (dbTypeCode)
        {
            case DbTypeCode.Decimal:
                return (EvaluateToDecimal(obj) == null);
            case DbTypeCode.Double:
                return (EvaluateToDouble(obj) == null);
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
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
            return new NumericalOperation(numOperator, instExpr1, instExpr2);
        }
        return new NumericalOperation(numOperator, instExpr1);
    }

    public ITypeExpression Clone(VariableArray varArray)
    {
        return CloneToNumerical(varArray);
    }

    public INumericalExpression CloneToNumerical(VariableArray varArray)
    {
        if (expr2 != null)
        {
            return new NumericalOperation(numOperator, expr1.CloneToNumerical(varArray), expr2.CloneToNumerical(varArray));
        }
        return new NumericalOperation(numOperator, expr1.CloneToNumerical(varArray));
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
        stringBuilder.AppendLine(tabs, "NumericalOperation(");
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
        throw new NotImplementedException("AppendToInstrAndLeavesList is not implemented for NumericalOperation");
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
