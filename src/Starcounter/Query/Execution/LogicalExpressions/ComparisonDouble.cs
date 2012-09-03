
using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using Sc.Server.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Starcounter.Binding;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a Double comparison which is an operation
/// with operands that are Double, Decimal, integer or unsigned integer expressions
/// and a result value of type TruthValue. At least one operand should be a Double
/// expression.
/// </summary>
internal class ComparisonDouble : CodeGenFilterNode, IComparison
{
    ComparisonOperator compOperator;
    INumericalExpression expr1;
    INumericalExpression expr2;
    DbTypeCode typeCode;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="compOp">The comparison operator of the operation.</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    internal ComparisonDouble(ComparisonOperator compOp, INumericalExpression expr1, INumericalExpression expr2)
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
        this.expr1 = expr1;
        this.expr2 = expr2;
        typeCode = DbTypeCode.Double;
    }

    public ComparisonOperator Operator
    {
        get
        {
            return compOperator;
        }
    }

    /// <summary>
    /// Calculates the truth value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The truth value of this operation when evaluated on the input object.</returns>
    public TruthValue Evaluate(IObjectView obj)
    {
        Nullable<Double> value1 = expr1.EvaluateToDouble(obj);
        Nullable<Double> value2 = expr2.EvaluateToDouble(obj);
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
    /// Creates an more instantiated copy of this expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public ILogicalExpression Instantiate(CompositeObject obj)
    {
        return new ComparisonDouble(compOperator, expr1.Instantiate(obj), expr2.Instantiate(obj));
    }

    /// <summary>
    /// Gets a path that eventually (if there is a corresponding index) can be used for
    /// an index scan for the extent with the input extent number, if there is such a path.
    /// </summary>
    /// <param name="extentNumber">Input extent number.</param>
    /// <returns>A path, if an appropriate path is found, otherwise null.</returns>
    public IPath GetIndexPath(Int32 extentNum)
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
        return new ComparisonDouble(compOperator, expr1.CloneToNumerical(varArray), expr2.CloneToNumerical(varArray));
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
            return new NumericalRangePoint(compOperator, expr2);
        }
        if (expr2 is IPath && (expr2 as IPath).ExtentNumber == extentNumber && (expr2 as IPath).FullName == strPath && Optimizer.ReversableOperator(compOperator))
        {
            return new NumericalRangePoint(Optimizer.ReverseOperator(compOperator), expr1);
        }
        return null;
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ComparisonDouble(");
        stringBuilder.AppendLine(tabs + 1, compOperator.ToString());
        expr1.BuildString(stringBuilder, tabs + 1);
        expr2.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        if (typeCode == DbTypeCode.Double)
        {
            return CodeAsStringGeneric(compOperator, "ComparisonDouble", "FLT8");
        }
        else if (typeCode == DbTypeCode.Single)
        {
            return CodeAsStringGeneric(compOperator, "ComparisonDouble", "FLT4");
        }
        return CodeAsStringGeneric(compOperator, "ComparisonDouble", "DEC");
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        if (typeCode == DbTypeCode.Double)
        {
            return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.FLT8_INCR, "ComparisonDouble");
        }
        else if (typeCode == DbTypeCode.Single)
        {
            return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.FLT4_INCR, "ComparisonDouble");
        }
        return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.DEC_INCR, "ComparisonDouble");
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
            if (expr1.DbTypeCode == DbTypeCode.Single)
            {
                typeCode = DbTypeCode.Single;
            }
            else
            {
                typeCode = DbTypeCode.Double;
            }

            // Same type of the data for underlying numerical variables.
            stackChangeLeft = expr1.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);
            stackChangeRight = expr2.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);
        }
        else
        {
            // Different underlying types so decimal as general type.
            typeCode = DbTypeCode.Decimal;

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
}
}
