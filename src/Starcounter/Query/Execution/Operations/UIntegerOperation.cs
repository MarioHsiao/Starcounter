// ***********************************************************************
// <copyright file="UIntegerOperation.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Starcounter.Binding;
using System.Diagnostics;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about an operation with result value of type unsigned integer.
/// </summary>
internal class UIntegerOperation : IUIntegerExpression, INumericalOperation
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
    internal UIntegerOperation(NumericalOperator numOp, INumericalExpression expr1, INumericalExpression expr2)
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
    /// <param name="expr">The operand of the operation.</param>
    internal UIntegerOperation(NumericalOperator numOp, INumericalExpression expr)
    : base()
    {
        if (numOp != NumericalOperator.Plus)
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
            return DbTypeCode.UInt64;
        }
    }

    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.UInteger;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        return (expr1.InvolvesCodeExecution() || (expr2 != null && expr2.InvolvesCodeExecution()));
    }

    /// <summary>
    /// Calculates the value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The value of this operation when evaluated on the input object.</returns>
    public Nullable<UInt64> EvaluateToUInteger(IObjectView obj)
    {
        Nullable<UInt64> value1 = expr1.EvaluateToUInteger(obj);
        Nullable<UInt64> value2 = null;
        if (expr2 != null)
        {
            value2 = expr2.EvaluateToUInteger(obj);
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
        return EvaluateToUInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this operation as a nullable Double.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Double> EvaluateToDouble(IObjectView obj)
    {
        return EvaluateToUInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this operation as a nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToInteger(IObjectView obj)
    {
        Nullable<UInt64> value = EvaluateToUInteger(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value > Int64.MaxValue)
        {
            return null;
        }
        return (Int64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this operation as a ceiling (round up) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj)
    {
        Nullable<UInt64> value = EvaluateToUInteger(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value > Int64.MaxValue)
        {
            return null;
        }
        return (Int64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this operation as a floor (round down) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj)
    {
        Nullable<UInt64> value = EvaluateToUInteger(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value > Int64.MaxValue)
        {
            return Int64.MaxValue;
        }
        return (Int64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this operation as a ceiling (round up) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj)
    {
        return EvaluateToUInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this operation as a floor (round down) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this operation.</returns>
    public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj)
    {
        return EvaluateToUInteger(obj);
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
        return (EvaluateToUInteger(obj) == null);
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a Row.
    /// Properties, with extent numbers for which there exist objects attached to the Row,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The Row on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public INumericalExpression Instantiate(Row obj)
    {
        if (expr2 != null)
        {
            return new UIntegerOperation(numOperator, (INumericalExpression)expr1.Instantiate(obj), (INumericalExpression)expr2.Instantiate(obj));
        }
        return new UIntegerOperation(numOperator, (INumericalExpression)expr1.Instantiate(obj));
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return CloneToUInteger(varArray);
    }

    public IUIntegerExpression CloneToUInteger(VariableArray varArray)
    {
        if (expr2 != null)
        {
            return new UIntegerOperation(numOperator, expr1.CloneToNumerical(varArray), expr2.CloneToNumerical(varArray));
        }
        return new UIntegerOperation(numOperator, expr1.CloneToNumerical(varArray));
    }

    public INumericalExpression CloneToNumerical(VariableArray varArray)
    {
        return CloneToUInteger(varArray);
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
        stringBuilder.AppendLine(tabs, "UIntegerOperation(");
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
        throw new NotImplementedException("AppendToInstrAndLeavesList is not implemented for UIntegerOperation");
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

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IValueExpression other) {
        UIntegerOperation otherNode = other as UIntegerOperation;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(UIntegerOperation other) {
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
        Debug.Assert(this.numOperator == other.numOperator);
        if (this.numOperator != other.numOperator)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.expr1 == null) {
            Debug.Assert(other.expr1 == null);
            areEquals = other.expr1 == null;
        } else
            areEquals = this.expr1.AssertEquals(other.expr1);
        if (areEquals)
            if (this.expr2 == null) {
                Debug.Assert(other.expr2 == null);
                areEquals = other.expr2 == null;
            } else
                areEquals = this.expr2.AssertEquals(other.expr2);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
