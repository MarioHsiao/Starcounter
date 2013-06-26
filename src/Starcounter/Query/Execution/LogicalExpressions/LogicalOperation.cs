// ***********************************************************************
// <copyright file="LogicalOperation.cs" company="Starcounter AB">
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
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a Logical operation which is an operation
/// with operands that are conditions and a result value of type TruthValue.
/// </summary>
internal class LogicalOperation : CodeGenFilterNode, ILogicalExpression
{
    LogicalOperator logOperator;
    ILogicalExpression condition1;
    ILogicalExpression condition2;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logOp">The Logical operator of the operation.</param>
    /// <param name="cond1">The first operand of the operation.</param>
    /// <param name="cond2">The second operand of the operation.</param>
    internal LogicalOperation(LogicalOperator logOp, ILogicalExpression cond1, ILogicalExpression cond2)
    : base()
    {
        if (logOp == LogicalOperator.NOT)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logOp: " + logOp);
        }
        if (cond1 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect cond1.");
        }
        if (cond2 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect cond2.");
        }
        logOperator = logOp;
        condition1 = cond1;
        condition2 = cond2;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logOp">The Logical operator of the operation.</param>
    /// <param name="cond">The operand of the operation.</param>
    internal LogicalOperation(LogicalOperator logOp, ILogicalExpression cond)
    : base()
    {
        if (logOp != LogicalOperator.NOT)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logOp: " + logOp);
        }
        if (cond == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect cond.");
        }
        logOperator = logOp;
        condition1 = cond;
        condition2 = null;
    }

    // Needed for Creator.TransformLogicalExpressionIntoList(ILogicalExpression, List<ILogicalExpression>).
    internal LogicalOperator Operator
    {
        get
        {
            return logOperator;
        }
    }

    /// <summary>
    /// Gets if both conditions can code gen.
    /// </summary>
    public override bool CanCodeGen {
        get {
            if ((condition1 is CodeGenFilterNode) && (condition2 is CodeGenFilterNode))
                return (condition1 as CodeGenFilterNode).CanCodeGen && (condition2 as CodeGenFilterNode).CanCodeGen;
            else
                return false;
        }
    }

    // Needed for Creator.TransformLogicalExpressionIntoList(ILogicalExpression, List<ILogicalExpression>).
    internal ILogicalExpression Expression1
    {
        get
        {
            return condition1;
        }
    }

    // Needed for Creator.TransformLogicalExpressionIntoList(ILogicalExpression, List<ILogicalExpression>).
    internal ILogicalExpression Expression2
    {
        get
        {
            return condition2;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        return (condition1.InvolvesCodeExecution() || (condition2 != null && condition2.InvolvesCodeExecution()));
    }

    /// <summary>
    /// Calculates the truth value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The truth value of this operation when evaluated on the input object.</returns>
    public TruthValue Evaluate(IObjectView obj)
    {
        TruthValue value1 = condition1.Evaluate(obj);
        TruthValue value2 = TruthValue.UNKNOWN;
        if (condition2 != null)
        {
            value2 = condition2.Evaluate(obj);
        }
        switch (logOperator)
        {
            case LogicalOperator.AND:
                if (value1 == TruthValue.TRUE && value2 == TruthValue.TRUE)
                {
                    return TruthValue.TRUE;
                }
                else if (value1 == TruthValue.FALSE || value2 == TruthValue.FALSE)
                {
                    return TruthValue.FALSE;
                }
                else
                {
                    return TruthValue.UNKNOWN;
                }
            case LogicalOperator.OR:
                if (value1 == TruthValue.TRUE || value2 == TruthValue.TRUE)
                {
                    return TruthValue.TRUE;
                }
                else if (value1 == TruthValue.FALSE && value2 == TruthValue.FALSE)
                {
                    return TruthValue.FALSE;
                }
                else
                {
                    return TruthValue.UNKNOWN;
                }
            case LogicalOperator.IS:
                if (value1 == value2)
                {
                    return TruthValue.TRUE;
                }
                else
                {
                    return TruthValue.FALSE;
                }
            case LogicalOperator.NOT:
                if (value1 == TruthValue.TRUE)
                {
                    return TruthValue.FALSE;
                }
                else if (value1 == TruthValue.FALSE)
                {
                    return TruthValue.TRUE;
                }
                else
                {
                    return TruthValue.UNKNOWN;
                }
            case LogicalOperator.XOR:
                if (value1 == TruthValue.UNKNOWN || value2 == TruthValue.UNKNOWN)
                {
                    return TruthValue.UNKNOWN;
                }
                else if (value1 == value2)
                {
                    return TruthValue.FALSE;
                }
                else
                {
                    return TruthValue.TRUE;
                }
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect operator: " + logOperator);
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
        if (condition2 != null)
        {
            return new LogicalOperation(logOperator, condition1.Instantiate(obj), condition2.Instantiate(obj));
        }
        return new LogicalOperation(logOperator, condition1.Instantiate(obj));
    }

    public ILogicalExpression Clone(VariableArray varArray)
    {
        if (condition2 != null)
        {
            return new LogicalOperation(logOperator, condition1.Clone(varArray), condition2.Clone(varArray));
        }
        return new LogicalOperation(logOperator, condition1.Clone(varArray));
    }

    public ExtentSet GetOutsideJoinExtentSet()
    {
        ExtentSet extentSet = condition1.GetOutsideJoinExtentSet();
        if (logOperator == LogicalOperator.NOT)
            return extentSet;

        if (extentSet == null)
            return condition2.GetOutsideJoinExtentSet();

        return extentSet.Union(condition2.GetOutsideJoinExtentSet());
    }

    public override void InstantiateExtentSet(ExtentSet extentSet)
    {
        condition1.InstantiateExtentSet(extentSet);
        if (condition2 != null)
        {
            condition2.InstantiateExtentSet(extentSet);
        }
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "LogicalOperation(");
        stringBuilder.AppendLine(tabs + 1, logOperator.ToString());
        condition1.BuildString(stringBuilder, tabs + 1);
        if (condition2 != null)
        {
            condition2.BuildString(stringBuilder, tabs + 1);
        }
        stringBuilder.AppendLine(tabs, ")");
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        switch (logOperator)
        {
            case LogicalOperator.AND:
                return "AND";
            case LogicalOperator.OR:
                return "OR";
            case LogicalOperator.IS:
                return "IS";
            case LogicalOperator.NOT:
                return "NOT";
            case LogicalOperator.XOR:
                return "XOR";
            default:
                throw new NotImplementedException("CodeAsString is not implemented for LogicalOperation");
        }
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        switch (logOperator)
        {
            case LogicalOperator.AND:
                return CodeGenFilterInstrCodes.AND;
            case LogicalOperator.OR:
                return CodeGenFilterInstrCodes.OR;
            case LogicalOperator.IS:
                throw new NotImplementedException("InstrCode()->IS is not implemented for LogicalOperation");
            case LogicalOperator.NOT:
                throw new NotImplementedException("InstrCode()->NOT is not implemented for LogicalOperation");
            case LogicalOperator.XOR:
                throw new NotImplementedException("InstrCode()->XOR is not implemented for LogicalOperation");
            default:
                throw new NotImplementedException("InstrCode() is not implemented for LogicalOperation");
        }
    }

    // Append this node to filter instructions and leaves.
    public override UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                                      CodeGenFilterInstrArray instrArray,
                                                      Int32 currentExtent,
                                                      StringBuilder filterText)
    {
        return AppendToInstrAndLeavesList(condition1, condition2, dataLeaves, instrArray, currentExtent, filterText);
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        condition1.GenerateCompilableCode(stringGen);
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, logOperator.ToString());
        condition2.GenerateCompilableCode(stringGen);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(ILogicalExpression other) {
        LogicalOperation otherNode = other as LogicalOperation;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(LogicalOperation other) {
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
        Debug.Assert(this.logOperator == other.logOperator);
        if (this.logOperator != other.logOperator)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.condition1 == null) {
            Debug.Assert(other.condition1 == null);
            areEquals = other.condition1 == null;
        } else
            areEquals = this.condition1.AssertEquals(other.condition1);
        if (areEquals)
            if (this.condition2 == null) {
                Debug.Assert(other.condition2 == null);
                areEquals = other.condition2 == null;
            } else
                areEquals = this.condition2.AssertEquals(other.condition2);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
