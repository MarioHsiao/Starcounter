// ***********************************************************************
// <copyright file="ComparisonObject.cs" company="Starcounter AB">
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
using Starcounter.Query.Execution;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about an object comparison which is an operation
/// with operands that are object references and a result value of type TruthValue.
/// </summary>
internal class ComparisonObject : CodeGenFilterNode, IComparison
{
    ComparisonOperator compOperator;
    IObjectExpression expr1;
    IObjectExpression expr2;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="compOp">The comparison operator of the operation.</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    internal ComparisonObject(ComparisonOperator compOp, IObjectExpression expr1, IObjectExpression expr2)
    {
        if (compOp != ComparisonOperator.Equal && compOp != ComparisonOperator.NotEqual &&
            compOp != ComparisonOperator.IS && compOp != ComparisonOperator.ISNOT)
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
        this.expr1 = expr1;
        this.expr2 = expr2;
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
        IObjectView value1 = expr1.EvaluateToObject(obj);
        IObjectView value2 = expr2.EvaluateToObject(obj);
        switch (compOperator)
        {
            case ComparisonOperator.Equal:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (value1.Equals(value2))
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.NotEqual:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (value1.Equals(value2))
                {
                    return TruthValue.FALSE;
                }
                return TruthValue.TRUE;
            case ComparisonOperator.IS:
                if (value1 == null && value2 == null)
                {
                    return TruthValue.TRUE;
                }
                if (value1 == null || value2 == null)
                {
                    return TruthValue.FALSE;
                }
                if (value1.Equals(value2))
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
                if (value1.Equals(value2))
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
        return new ComparisonObject(compOperator, expr1.Instantiate(obj), expr2.Instantiate(obj));
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
        return new ComparisonObject(compOperator, expr1.CloneToObject(varArray), expr2.CloneToObject(varArray));
    }

    public IObjectExpression GetReferenceLookUpExpression(Int32 extentNumber)
    {
        if (compOperator == ComparisonOperator.Equal)
        {
            if ((expr1 is ObjectThis) && (expr1 as ObjectThis).ExtentNumber == extentNumber)
            {
                // Control there is no reference to the current extent (extentNum) in the other expression.
                ExtentSet extentSet = new ExtentSet();
                expr2.InstantiateExtentSet(extentSet);
                if (!extentSet.IncludesExtentNumber(extentNumber))
                {
                    return expr2;
                }
            }
            if ((expr2 is ObjectThis) && (expr2 as ObjectThis).ExtentNumber == extentNumber)
            {
                // Control there is no reference to the current extent (extentNum) in the other expression.
                ExtentSet extentSet = new ExtentSet();
                expr1.InstantiateExtentSet(extentSet);
                if (!extentSet.IncludesExtentNumber(extentNumber))
                {
                    return expr1;
                }
            }
        }
        return null;
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
        if (expr1 is IPath && (expr1 as IPath).ExtentNumber == extentNumber && (expr1 as IPath).FullName == strPath)
        {
            return new ObjectRangePoint(compOperator, expr2);
        }
        if (expr2 is IPath && (expr2 as IPath).ExtentNumber == extentNumber && (expr2 as IPath).FullName == strPath && Optimizer.ReversableOperator(compOperator))
        {
            return new ObjectRangePoint(Optimizer.ReverseOperator(compOperator), expr1);
        }
        return null;
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ComparisonObject(");
        stringBuilder.AppendLine(tabs + 1, compOperator.ToString());
        expr1.BuildString(stringBuilder, tabs + 1);
        expr2.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return CodeAsStringGeneric(compOperator, "ComparisonObject", "REF");
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.REF_INCR, "ComparisonObject");
    }

    // Append this node to filter instructions and leaves.
    public override UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                                      CodeGenFilterInstrArray instrArray,
                                                      Int32 currentExtent,
                                                      StringBuilder filterText)
    {
        return AppendToInstrAndLeavesList(expr1, expr2, dataLeaves, instrArray, currentExtent, filterText);
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
        ComparisonObject otherNode = other as ComparisonObject;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(ComparisonObject other) {
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
            if (this.expr1 == null) {
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
