// ***********************************************************************
// <copyright file="ComparisonDecimal.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a Decimal comparison which is an operation
/// with operands that are Decimal, integer or unsigned integer expressions and a
/// result value of type TruthValue.
/// </summary>
internal class ComparisonDecimal : CodeGenFilterNode, IComparison
{
    ComparisonOperator compOperator;
    ExtentSet outsideJoinExtentSet; // Used to handle IS and ISNOT comparisons w.r.t. outer joins.
    INumericalExpression expr1;
    INumericalExpression expr2;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="compOp">The comparison operator of the operation.</param>
    /// <param name="extSet">A set of extents where this comparison cannot be executed 
    /// (only relevant when operator is IS or ISNOT and there is an outer join).</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    internal ComparisonDecimal(ComparisonOperator compOp, ExtentSet extSet, INumericalExpression expr1, INumericalExpression expr2)
    {
        if (compOp == ComparisonOperator.LIKEdynamic || compOp == ComparisonOperator.LIKEstatic)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp.");
        }
        if (expr1 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
        }
        if (expr2 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr2.");
        }
        compOperator = compOp;
        outsideJoinExtentSet = extSet;
        this.expr1 = expr1;
        this.expr2 = expr2;
        throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "ComparisonDecimal is not supported. ComparisonNumerical should be used.");
    }

    public ComparisonOperator Operator
    {
        get
        {
            return compOperator;
        }
    }

    /// <summary>
    /// Gets if both expressions can code gen.
    /// </summary>
    public override bool CanCodeGen {
        get {
            if ((expr1 is CodeGenFilterNode) && (expr2 is CodeGenFilterNode))
                return (expr1 as CodeGenFilterNode).CanCodeGen && (expr2 as CodeGenFilterNode).CanCodeGen;
            else
                return false;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        return (expr1.InvolvesCodeExecution() || expr2.InvolvesCodeExecution());
    }

    /// <summary>
    /// Calculates the truth value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The truth value of this operation when evaluated on the input object.</returns>
    public TruthValue Evaluate(IObjectView obj)
    {
        Nullable<Decimal> value1 = expr1.EvaluateToDecimal(obj);
        Nullable<Decimal> value2 = expr2.EvaluateToDecimal(obj);
        switch (compOperator)
        {
            case ComparisonOperator.Equal:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (value1.Value.CompareTo(value2.Value) == 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.NotEqual:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (value1.Value.CompareTo(value2.Value) != 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.LessThan:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (value1.Value.CompareTo(value2.Value) < 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.LessThanOrEqual:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (value1.Value.CompareTo(value2.Value) <= 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.GreaterThan:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (value1.Value.CompareTo(value2.Value) > 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.GreaterThanOrEqual:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (value1.Value.CompareTo(value2.Value) >= 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.IS:
                if (value1 == null && value2 == null)
                {
                    return TruthValue.TRUE;
                }
                if (value1 == null || value2 == null)
                {
                    return TruthValue.FALSE;
                }
                if (value1.Value.CompareTo(value2.Value) == 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.ISNOT:
                if (value1 == null && value2 == null)
                {
                    return TruthValue.FALSE;
                }
                if (value1 == null || value2 == null)
                {
                    return TruthValue.TRUE;
                }
                if (value1.Value.CompareTo(value2.Value) == 0)
                {
                    return TruthValue.FALSE;
                }
                return TruthValue.TRUE;
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOperator: " + compOperator);
        }
    }

    /// <summary>
    /// Calculates the Boolean value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The Boolean value of this operation when evaluated on the input object.</returns>
    public Boolean Filtrate(IObjectView obj)
    {
        return Evaluate(obj) == TruthValue.TRUE;
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a Row.
    /// Properties, with extent numbers for which there exist objects attached to the Row,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The Row on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public ILogicalExpression Instantiate(Row obj)
    {
        return new ComparisonDecimal(compOperator, outsideJoinExtentSet, expr1.Instantiate(obj), expr2.Instantiate(obj));
    }

    /// <summary>
    /// Gets a path to the given extent.
    /// The path is used for an index scan for the extent with the input extent number, 
    /// if there is such a path and if there is a corresponding index.
    /// </summary>
    /// <param name="extentNum">Input extent number.</param>
    /// <returns>A path, if an appropriate path is found, otherwise null.</returns>
    public IPath GetPathTo(Int32 extentNum)
    {
        // Control if the comparison operator allows an eventual path to be used in an index scan.
        if (!Optimizer.RangeOperator(compOperator))
        {
            return null;
        }
        if (expr1 is IPath && (expr1 as IPath).ExtentNumber == extentNum)
        {
            // Control there is no reference to the current extent (extentNum) in the other expression.
            ExtentSet extentSet = new ExtentSet();
            expr2.InstantiateExtentSet(extentSet);
            if (!extentSet.IncludesExtentNumber(extentNum))
            {
                return (expr1 as IPath);
            }
        }
        if (expr2 is IPath && (expr2 as IPath).ExtentNumber == extentNum)
        {
            // Control there is no reference to the current extent (extentNum) in the other expression.
            ExtentSet extentSet = new ExtentSet();
            expr1.InstantiateExtentSet(extentSet);
            if (!extentSet.IncludesExtentNumber(extentNum))
            {
                return (expr2 as IPath);
            }
        }
        return null;
    }

    public ILogicalExpression Clone(VariableArray varArray)
    {
        return new ComparisonDecimal(compOperator, outsideJoinExtentSet, expr1.CloneToNumerical(varArray), expr2.CloneToNumerical(varArray));
    }

    public ExtentSet GetOutsideJoinExtentSet()
    {
        return outsideJoinExtentSet;
    }

    public override void InstantiateExtentSet(ExtentSet extentSet)
    {
        expr1.InstantiateExtentSet(extentSet);
        expr2.InstantiateExtentSet(extentSet);
    }

    public RangePoint CreateRangePoint(Int32 extentNumber, String strPath)
    {
        if (!Optimizer.RangeOperator(compOperator))
        {
            return null;
        }
        if (expr1 is IPath && (expr1 as IPath).ExtentNumber == extentNumber && (expr1 as IPath).ColumnName == strPath)
        {
            return new NumericalRangePoint(compOperator, expr2);
        }
        if (expr2 is IPath && (expr2 as IPath).ExtentNumber == extentNumber && (expr2 as IPath).ColumnName == strPath && Optimizer.ReversableOperator(compOperator))
        {
            return new NumericalRangePoint(Optimizer.ReverseOperator(compOperator), expr1);
        }
        return null;
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ComparisonDecimal(");
        stringBuilder.AppendLine(tabs + 1, compOperator.ToString());
        expr1.BuildString(stringBuilder, tabs + 1);
        expr2.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    // Append this node to filter instructions and leaves.
    public override UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                                      CodeGenFilterInstrArray instrArray,
                                                      Int32 currentExtent,
                                                      StringBuilder filterText)
    {
        UInt32 stackChangeLeft = 0, stackChangeRight = 0;

        // Checking the data type for the sub-nodes.
        if (ComparisonNumerical.GroupSameNumericalTypes(expr1.DbTypeCode) ==
            ComparisonNumerical.GroupSameNumericalTypes(expr2.DbTypeCode))
        {
            // Same type of the data for underlying numerical variables.
            stackChangeLeft = expr1.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);
            stackChangeRight = expr2.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);
        }
        else
        {
            // Processing first operand.
            stackChangeLeft = expr1.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);

            // Adding conversion instruction.
            if (instrArray != null)
            {
                String convCodeStr = ComparisonNumerical.AddConversionInstr(instrArray, expr1.DbTypeCode);
                if (filterText != null)
                {
                    filterText.Append(convCodeStr);
                }
            }

            // Processing second operand.
            stackChangeRight = expr2.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);

            // Adding conversion instruction.
            if (instrArray != null)
            {
                String convCodeStr = ComparisonNumerical.AddConversionInstr(instrArray, expr2.DbTypeCode);
                if (filterText != null)
                {
                    filterText.Append(convCodeStr);
                }
            }
        }

        UInt32 newInstrCode = InstrCode();
        if (instrArray != null)
        {
            instrArray.Add(newInstrCode);
        }

        if (filterText != null)
        {
            filterText.Append(InstrCode() + ": " + CodeAsString() + "\n");
        }

        // Returning total stack change.
        return stackChangeLeft + stackChangeRight + StackChange();
    }


    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return CodeAsStringGeneric(compOperator, "ComparisonDecimal", "DEC");
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.DEC_INCR, "ComparisonDecimal");
    }

    /// <summary>
    /// Appends operation type to the node type list.
    /// </summary>
    /// <param name="nodeTypeList">List with condition nodes types.</param>
    public override void AddNodeTypeToList(List<ConditionNodeType> nodeTypeList)
    {
        // Calling the base function.
        AddNodeCompTypeToList(compOperator, expr1, expr2, nodeTypeList);
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expr1.GenerateCompilableCode(stringGen);
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, " " + compOperator.ToString() + " ");
        expr2.GenerateCompilableCode(stringGen);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(ILogicalExpression other) {
        ComparisonDecimal otherNode = other as ComparisonDecimal;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(ComparisonDecimal other) {
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
        Debug.Assert(this.compOperator == other.compOperator);
        if (this.compOperator != other.compOperator)
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
