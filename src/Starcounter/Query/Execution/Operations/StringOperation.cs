// ***********************************************************************
// <copyright file="StringOperation.cs" company="Starcounter AB">
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


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about an operation with result value of type
/// String.
/// </summary>
internal class StringOperation : IStringExpression, IOperation
{
    StringOperator strOperator;
    IStringExpression expr1;
    IStringExpression expr2;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="strOp">The operator of the operation.</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    internal StringOperation(StringOperator strOp, IStringExpression expr1, IStringExpression expr2)
    : base()
    {
        if (strOp != StringOperator.Concatenation)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect strOp: " + strOp);
        }
        if (expr1 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
        }
        if (expr2 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr2.");
        }
        strOperator = strOp;
        this.expr1 = expr1;
        this.expr2 = expr2;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="strOp">The operator of the operation.</param>
    /// <param name="expr">The operand of the operation.</param>
    internal StringOperation(StringOperator strOp, IStringExpression expr)
    : base()
    {
        if (strOp != StringOperator.AppendMaxChar)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect strOp: " + strOp);
        }
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
        }
        strOperator = strOp;
        expr1 = expr;
        expr2 = null;
    }

    /// <summary>
    /// Controls if the current operator is AppendMaxChar.
    /// </summary>
    /// <returns>True, if current operator is AppendMaxChar, otherwise false.</returns>
    internal Boolean AppendMaxChar()
    {
        return (strOperator == StringOperator.AppendMaxChar);
    }

    /// <summary>
    /// The DbTypeCode of the result of the operation.
    /// </summary>
    public DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.String;
        }
    }

    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.String;
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
    public String EvaluateToString(IObjectView obj)
    {
        String value1 = expr1.EvaluateToString(obj);
        String value2 = null;
        if (expr2 != null)
        {
            value2 = expr2.EvaluateToString(obj);
        }
        switch (strOperator)
        {
            case StringOperator.Concatenation:
                if (value1 == null || value2 == null)
                {
                    return null;
                }
                return value1 + value2;
            case StringOperator.AppendMaxChar:
                return value1; // The operation AppendMaxChar must be handled by the caller.
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect operator: " + strOperator);
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
        return (EvaluateToString(obj) == null);
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public IStringExpression Instantiate(CompositeObject obj)
    {
        if (expr2 != null)
        {
            return new StringOperation(strOperator, expr1.Instantiate(obj), expr2.Instantiate(obj));
        }
        return new StringOperation(strOperator, expr1.Instantiate(obj));
    }

    public ITypeExpression Clone(VariableArray varArray)
    {
        return CloneToString(varArray);
    }

    public IStringExpression CloneToString(VariableArray varArray)
    {
        if (expr2 != null)
        {
            return new StringOperation(strOperator, expr1.CloneToString(varArray), expr2.CloneToString(varArray));
        }
        return new StringOperation(strOperator, expr1.CloneToString(varArray));
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
        stringBuilder.AppendLine(tabs, "StringOperation(");
        stringBuilder.AppendLine(tabs + 1, strOperator.ToString());
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
        throw new NotImplementedException("AppendToInstrAndLeavesList is not implemented for StringOperation");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expr1.GenerateCompilableCode(stringGen);
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, " " + strOperator.ToString() + " ");
        expr1.GenerateCompilableCode(stringGen);
    }
}
}
