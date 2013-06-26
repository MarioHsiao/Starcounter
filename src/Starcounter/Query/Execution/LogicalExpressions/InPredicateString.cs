// ***********************************************************************
// <copyright file="InPredicateString.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using System.Text;
using Starcounter.Binding;
using System.Diagnostics;


namespace Starcounter.Query.Execution
{
// TODO: Test this class. Not in use.

/// <summary>
/// Class that holds information about an in-predicate where the left argument is
/// a String expression and the right argument is a set of String expressions.
/// </summary>
internal class InPredicateString : ILogicalExpression
{
    IStringExpression expression;
    List<IStringExpression> exprList;

    /// <summary>
    /// The DbTypeCode of the value of the expression or the property.
    /// </summary>
    public DbTypeCode DbTypeCode
    {
        get
        {
            throw new NotImplementedException("DbTypeCode is not implemented for InPredicateString");
        }
    }

    /// <summary>
    /// Examines if the value of the expression is null when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>True, if the value of the expression when evaluated on the input object
    /// is null, otherwise false.</returns>
    public Boolean EvaluatesToNull(IObjectView obj)
    {
        throw new NotImplementedException("EvaluatesToNull is not implemented for InPredicateString");
    }

    // Append this node to filter instructions and leaves.
    // Called statically so no need to worry about performance.
    // Need to redefine for leaves (this implementation is for intermediate nodes).
    public UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                             CodeGenFilterInstrArray instrArray,
                                             Int32 currentExtent,
                                             StringBuilder filterText)
    {
        throw new NotImplementedException("AppendToInstrAndLeavesList is not implemented for InPredicateString");
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="expr">The left argument to the in-predicate.</param>
    /// <param name="list">The right argument to the in-predicate.</param>
    internal InPredicateString(IStringExpression expr, List<IStringExpression> list)
    : base()
    {
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        if (list == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect currentLogExprList.");
        }
        expression = expr;
        exprList = list;
    }

    public Boolean InvolvesCodeExecution()
    {
        Boolean codeExecution = expression.InvolvesCodeExecution();
        Int32 i = 0;
        while (codeExecution == false && i < exprList.Count)
        {
            codeExecution = exprList[i].InvolvesCodeExecution();
            i++;
        }
        return codeExecution;
    }

    /// <summary>
    /// Calculates the truth value of this in-predicate when evaluated on an input object.
    /// All properties in this in-predicate are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this in-predicate.</param>
    /// <returns>The truth value of this in-predicate when evaluated on the input object.</returns>
    public TruthValue Evaluate(IObjectView obj)
    {
        String value1 = expression.EvaluateToString(obj);
        TruthValue totalTruthValue = TruthValue.FALSE;
        Int32 i = 0;
        while (i < exprList.Count && totalTruthValue != TruthValue.TRUE)
        {
            String value2 = exprList[i].EvaluateToString(obj);
            TruthValue truthValue = TruthValue.FALSE;
            if (value1 == null || value2 == null)
            {
                truthValue = TruthValue.UNKNOWN;
            }
            else if (value1.CompareTo(value2) == 0)
            {
                truthValue = TruthValue.TRUE;
            }
            if (totalTruthValue == TruthValue.TRUE || truthValue == TruthValue.TRUE)
            {
                totalTruthValue = TruthValue.TRUE;
            }
            else if (totalTruthValue == TruthValue.UNKNOWN || truthValue == TruthValue.UNKNOWN)
            {
                totalTruthValue = TruthValue.UNKNOWN;
            }
            else
            {
                totalTruthValue = TruthValue.FALSE;
            }
            i++;
        }
        return totalTruthValue;
    }

    /// <summary>
    /// Calculates the Boolean value of this in-predicate when evaluated on an input object.
    /// All properties in this in-predicate are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this in-predicate.</param>
    /// <returns>The Boolean value of this in-predicate when evaluated on the input object.</returns>
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
        IStringExpression instExpr = null;
        List<IStringExpression> instExprList = new List<IStringExpression>();
        instExpr = expression.Instantiate(obj);
        for (Int32 i = 0; i < exprList.Count; i++)
        {
            instExprList.Add(exprList[i].Instantiate(obj));
        }
        if (instExpr != null && instExprList != null)
        {
            return new InPredicateString(instExpr, instExprList);
        }
        else
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expression.");
        }
    }

    public ILogicalExpression Clone(VariableArray varArray)
    {
        List<IStringExpression> exprListClone = new List<IStringExpression>();
        for (Int32 i = 0; i < exprList.Count; i++)
        {
            exprListClone.Add(exprList[i].CloneToString(varArray));
        }
        return new InPredicateString(expression.CloneToString(varArray), exprListClone);
    }

    public ExtentSet GetOutsideJoinExtentSet()
    {
        return null;
    }

    public void InstantiateExtentSet(ExtentSet extentSet)
    {
        expression.InstantiateExtentSet(extentSet);
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "InPredicateString(");
        expression.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs + 1, "[");
        for (Int32 i = 0; i < exprList.Count; i++)
        {
            exprList[i].BuildString(stringBuilder, tabs + 2);
        }
        stringBuilder.AppendLine(tabs + 1, "]");
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expression.GenerateCompilableCode(stringGen);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(ILogicalExpression other) {
        InPredicateString otherNode = other as InPredicateString;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(InPredicateString other) {
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
        // Check cardinalities of collections
        Debug.Assert(this.exprList.Count == other.exprList.Count);
        if (this.exprList.Count != other.exprList.Count)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.expression == null) {
            Debug.Assert(other.expression == null);
            areEquals = other.expression == null;
        } else
            areEquals = this.expression.AssertEquals(other.expression);
        // Check collections of objects
        for (int i = 0; i < this.exprList.Count && areEquals; i++)
            areEquals = this.exprList[i].AssertEquals(other.exprList[i]);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
