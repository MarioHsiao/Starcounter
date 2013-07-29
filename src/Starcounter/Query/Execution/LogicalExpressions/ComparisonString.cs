// ***********************************************************************
// <copyright file="ComparisonString.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a String comparison which is an operation
/// with operands that are String expressions and a result value of type TruthValue.
/// </summary>
internal class ComparisonString : CodeGenFilterNode, IComparison
{
    ComparisonOperator compOperator;
    ExtentSet outsideJoinExtentSet; // Used to handle IS and ISNOT comparisons w.r.t. outer joins.
    IStringExpression expr1;
    IStringExpression expr2;
    IStringExpression expr3;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="compOp">The comparison operator of the operation.</param>
    /// <param name="extSet">A set of extents where this comparison cannot be executed 
    /// (only relevant when operator is IS or ISNOT and there is an outer join).</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    internal ComparisonString(ComparisonOperator compOp, ExtentSet extSet, IStringExpression expr1, IStringExpression expr2)
    {
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
        expr3 = null;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="compOp">The comparison operator of the operation.</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    /// <param name="expr3">The third operand of the operation.</param>
    internal ComparisonString(ComparisonOperator compOp, IStringExpression expr1, IStringExpression expr2, IStringExpression expr3)
    {
        if (expr1 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
        }
        if (expr2 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr2.");
        }
        if (expr3 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr3.");
        }
        compOperator = compOp;
        outsideJoinExtentSet = null;
        this.expr1 = expr1;
        this.expr2 = expr2;
        this.expr3 = expr3;
    }

    public ComparisonOperator Operator
    {
        get
        {
            return compOperator;
        }
    }

    /// <summary>
    /// Gets if all three expressions can code gen or third expression is null.
    /// </summary>
    public override bool CanCodeGen {
        get {
                Boolean can = true;
                if (expr1 != null)
                    if (expr1 is CodeGenFilterNode)
                        can = can && (expr1 as CodeGenFilterNode).CanCodeGen;
                    else return false;
                if (expr2 != null)
                    if (expr2 is CodeGenFilterNode)
                        can = can && (expr2 as CodeGenFilterNode).CanCodeGen;
                    else return false;
                if (expr3 != null)
                    if (expr3 is CodeGenFilterNode)
                        can = can && (expr3 as CodeGenFilterNode).CanCodeGen;
                    else return false;
                return can;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        return (expr1.InvolvesCodeExecution() || expr2.InvolvesCodeExecution());
    }

    /// <summary>
    /// Handle the special string comparison "string1 &lt; AppendMaxChar(string2)".
    /// Operator AppendMaxChar is used to handle upper end-point in "STARTS WITH" intervals.
    /// </summary>
    /// <param name="str1">First string to compare.</param>
    /// <param name="str2">Second string to compare.</param>
    /// <returns>True, if "str1 &lt; AppendMaxChar(str2)", otherwise false.</returns>
    internal Boolean SpecialLessThan(String str1, String str2)
    {
        String temp = (str1.Length < str2.Length) ? str1 : str1.Substring(0, str2.Length);
        return (DbHelper.StringCompare(temp, str2) <= 0);
    }

    /// <summary>
    /// Calculates the truth value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The truth value of this operation when evaluated on the input object.</returns>
    public TruthValue Evaluate(IObjectView obj)
    {
        String value1 = expr1.EvaluateToString(obj);
        String value2 = expr2.EvaluateToString(obj);
        String value3 = null;
        if (expr3 != null)
        {
            value3 = expr3.EvaluateToString(obj);
        }
        switch (compOperator)
        {
            case ComparisonOperator.Equal:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (DbHelper.StringCompare(value1, value2) == 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.NotEqual:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (DbHelper.StringCompare(value1, value2) != 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.LessThan:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                // Handle special case "value1 AppendMaxChar(value2)".
                if (expr2 is StringOperation && (expr2 as StringOperation).AppendMaxChar())
                {
                    if (SpecialLessThan(value1, value2))
                    {
                        return TruthValue.TRUE;
                    }
                    return TruthValue.FALSE;
                }
                if (DbHelper.StringCompare(value1, value2) < 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.LessThanOrEqual:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (DbHelper.StringCompare(value1, value2) <= 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.GreaterThan:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (DbHelper.StringCompare(value1, value2) > 0)
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
            case ComparisonOperator.GreaterThanOrEqual:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (DbHelper.StringCompare(value1, value2) >= 0)
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
                if (DbHelper.StringCompare(value1, value2) == 0)
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
                if (DbHelper.StringCompare(value1, value2) == 0)
                {
                    return TruthValue.FALSE;
                }
                return TruthValue.TRUE;
            case ComparisonOperator.LIKEdynamic:
            case ComparisonOperator.LIKEstatic:
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                value2 = TransformPattern(value2, value3);
                if (Like(value1, value2))
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;

// TODO: LIKEstatic does not work properly with caching.
/*
            case ComparisonOperator.LIKEstatic:
                if (!(expr2 is StringLiteral) || !(expr2 as StringLiteral).IsPreEvaluatedPattern)
                {
                    expr2 = new StringLiteral(TransformPattern(value2, value3), true);
                }
                if (value1 == null || value2 == null)
                {
                    return TruthValue.UNKNOWN;
                }
                if (Like(value1, value2))
                {
                    return TruthValue.TRUE;
                }
                return TruthValue.FALSE;
*/

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
        if (expr3 != null)
        {
            return new ComparisonString(compOperator, expr1.Instantiate(obj), expr2.Instantiate(obj), expr3.Instantiate(obj));
        }
        return new ComparisonString(compOperator, outsideJoinExtentSet, expr1.Instantiate(obj), expr2.Instantiate(obj));
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

    private String TransformPattern(String pattern, String escape)
    {
        // Escape regular expression operator characters.
        pattern = pattern.Replace("\\", "\\\\");
        pattern = pattern.Replace(".", "\\.");
        pattern = pattern.Replace("$", "\\$");
        pattern = pattern.Replace("^", "\\^");
        pattern = pattern.Replace("{", "\\{");
        pattern = pattern.Replace("[", "\\[");
        pattern = pattern.Replace("(", "\\(");
        pattern = pattern.Replace("|", "\\|");
        pattern = pattern.Replace(")", "\\)");
        pattern = pattern.Replace("*", "\\*");
        pattern = pattern.Replace("+", "\\+");
        pattern = pattern.Replace("?", "\\?");
        // Replace SQL pattern operators with regular expression operators.
        pattern = pattern.Replace("%", ".*");
        pattern = pattern.Replace("_", ".");
        // Apply input escape character (or escape string).
        if (!String.IsNullOrEmpty(escape))
        {
            pattern = pattern.Replace(escape + ".*", "%");
            pattern = pattern.Replace(escape + ".", "_");
            pattern = pattern.Replace(escape, "");
        }
        // The match must occur at the beginning of the string.
        pattern = "^" + pattern;
        return pattern;
    }

    private Boolean Like(String value, String pattern)
    {
        Regex regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        Match match = regex.Match(value);
        return match.Success && match.Length == value.Length;
    }

    public ILogicalExpression Clone(VariableArray varArray)
    {
        if (expr3 != null)
        {
            return new ComparisonString(compOperator, expr1.CloneToString(varArray), expr2.CloneToString(varArray), expr3.CloneToString(varArray));
        }
        return new ComparisonString(compOperator, outsideJoinExtentSet, expr1.CloneToString(varArray), expr2.CloneToString(varArray));
    }

    public ExtentSet GetOutsideJoinExtentSet()
    {
        return outsideJoinExtentSet;
    }

    public override void InstantiateExtentSet(ExtentSet extentSet)
    {
        expr1.InstantiateExtentSet(extentSet);
        expr2.InstantiateExtentSet(extentSet);
        if (expr3 != null)
        {
            expr3.InstantiateExtentSet(extentSet);
        }
    }

    public RangePoint CreateRangePoint(Int32 extentNumber, String strPath)
    {
        if (!Optimizer.RangeOperator(compOperator))
        {
            return null;
        }
        if (expr1 is IPath && (expr1 as IPath).ExtentNumber == extentNumber && (expr1 as IPath).ColumnName == strPath)
        {
            return new StringRangePoint(compOperator, expr2);
        }
        if (expr2 is IPath && (expr2 as IPath).ExtentNumber == extentNumber && (expr2 as IPath).ColumnName == strPath && Optimizer.ReversableOperator(compOperator))
        {
            return new StringRangePoint(Optimizer.ReverseOperator(compOperator), expr1);
        }
        return null;
    }

    /// <summary>
    /// Gets a string expression to which ObjectID of this extent is equivalent.
    /// </summary>
    /// <param name="extentNumber"></param>
    /// <returns></returns>
    internal IStringExpression GetObjectIDExpression(int extentNumber) {
        if (compOperator == ComparisonOperator.Equal) {
            IStringExpression expr = null;
            if (expr1 is IProperty && (expr1 as IProperty).ExtentNumber == extentNumber && (expr1 as IProperty).FullName == DbHelper.ObjectIDName)
                expr = expr2;
            if (expr2 is IProperty && (expr2 as IProperty).ExtentNumber == extentNumber && (expr2 as IProperty).FullName == DbHelper.ObjectIDName)
                if (expr != null)
                    return null; // Both expressions are ObjectID properties on the same extent, i.e, self referencing.
                else
                    expr = expr1;
            return expr; // Assuming only useful expressions can be found.
        }
        return null;
    }
    
    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ComparisonString(");
        stringBuilder.AppendLine(tabs + 1, compOperator.ToString());
        expr1.BuildString(stringBuilder, tabs + 1);
        expr2.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return CodeAsStringGeneric(compOperator, "ComparisonString", "STR");
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.STR_INCR, "ComparisonString");
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
        // Calling the template comparison function.
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
        ComparisonString otherNode = other as ComparisonString;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(ComparisonString other) {
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
        if (areEquals)
            if (this.expr3 == null) {
                Debug.Assert(other.expr3 == null);
                areEquals = other.expr3 == null;
            } else
                areEquals = this.expr3.AssertEquals(other.expr3);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
