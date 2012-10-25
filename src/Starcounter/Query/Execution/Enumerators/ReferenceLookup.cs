// ***********************************************************************
// <copyright file="ReferenceLookup.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class ReferenceLookup : ExecutionEnumerator, IExecutionEnumerator
{
    Int32 extentNumber;
    CompositeObject contextObject;

    IObjectExpression expression;
    ILogicalExpression condition;

    internal ReferenceLookup(CompositeTypeBinding compTypeBind,
        Int32 extNum,
        IObjectExpression expr,
        ILogicalExpression cond,
        INumericalExpression fetchNumExpr,
        VariableArray varArr, String query)
        : base(varArr)
    {
        if (compTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compTypeBind.");
        if (expr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr.");
        if (cond == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect cond.");
        if (varArr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect variables clone.");

        compTypeBinding = compTypeBind;
        extentNumber = extNum;

        currentObject = null;
        contextObject = null;
        expression = expr;
        condition = cond;

        fetchNumberExpr = fetchNumExpr;

        this.query = query;
        variableArray = varArr;
    }

    /// <summary>
    /// The type binding of the resulting objects of the query.
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            if (singleObject)
                return compTypeBinding.GetPropertyBinding(0).TypeBinding;

            return compTypeBinding;
        }
    }

    Object IEnumerator.Current
    {
        get
        {
            return Current;
        }
    }

    public dynamic Current
    {
        get
        {
            if (currentObject != null)
            {
                if (singleObject)
                    return currentObject.GetObject(0);

                return currentObject;
            }

            throw new InvalidOperationException("Enumerator has not started or has already finished.");
        }
    }

    public CompositeObject CurrentCompositeObject
    {
        get
        {
            if (currentObject != null)
                return currentObject;

            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect currentObject.");   
        }
    }

    public Int32 Depth
    {
        get
        {
            return 0;
        }
    }

    public Boolean MoveNext()
    {
        if (counter == 0)
        {
            if (fetchNumberExpr != null && (fetchNumberExpr.EvaluateToInteger(null) == null || fetchNumberExpr.EvaluateToInteger(null).Value <= 0))
            {
                currentObject = null;
                return false;
            }

            //// Instantiate expression.
            //IObjectExpression instExpression = expression.Instantiate(contextObject);
            //// Lookup object.
            //IObjectView obj = null;
            //if ((instExpression is ObjectLiteral) || (instExpression is ObjectVariable))
            //{
            //    obj = instExpression.EvaluateToObject(null);
            //}
            //else
            //{
            //    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect instExpression: " + instExpression);
            //}

            // Lookup object.
            IObjectView obj = expression.EvaluateToObject(contextObject);

            // Check for null, that the object is in the current extent and check condition.
            if (obj == null || InCurrentExtent(obj) == false || condition.Instantiate(contextObject).Filtrate(obj) == false)
            {
                currentObject = null;
                return false;
            }
            else
            {
                // Create new currentObject.
                currentObject = new CompositeObject(compTypeBinding);
                currentObject.AttachObject(extentNumber, obj);
                counter++;
                return true;
            }
        }
        else
        {
            currentObject = null;
            return false;
        }
    }

    public Boolean MoveNextSpecial(Boolean force)
    {
        if (!force && MoveNext())
        {
            return true;
        }
        else if (counter == 0 || force)
        {
            // Create a NullObject.
            NullObject nullObj = new NullObject(compTypeBinding.GetTypeBinding(extentNumber));
            currentObject = new CompositeObject(compTypeBinding);
            currentObject.AttachObject(extentNumber, nullObj);
            counter++;
            return true;
        }
        else
        {
            currentObject = null;
            return false;
        }
    }

    /// <summary>
    /// Saves the underlying enumerator state.
    /// </summary>
    public unsafe Int32 SaveEnumerator(Byte* keysData, Int32 globalOffset, Boolean saveDynamicDataOnly)
    {
        return globalOffset;
    }

    /// <summary>
    /// Resets the enumerator with a context object.
    /// </summary>
    /// <param name="obj">Context object from another enumerator.</param>
    public override void Reset(CompositeObject obj)
    {
        contextObject = obj;
        currentObject = null;
        counter = 0;
    }

    /// <summary>
    /// Controls that the object obj is in the current extent (the extent of this reference-lookup).
    /// The object's type must be equal to or a subtype of the extent type.
    /// </summary>
    /// <param name="obj">The object to control.</param>
    /// <returns>True, if the object is in the current extent, otherwise false.</returns>
    internal Boolean InCurrentExtent(IObjectView obj)
    {
        if (compTypeBinding.GetTypeBinding(extentNumber) is TypeBinding && obj.TypeBinding is TypeBinding)
        {
            TypeBinding extentTypeBind = compTypeBinding.GetTypeBinding(extentNumber) as TypeBinding;
            TypeBinding tmpTypeBind = obj.TypeBinding as TypeBinding;
            return tmpTypeBind.SubTypeOf(extentTypeBind);
        }
        else
        {
            return false;
        }
    }

    public override IExecutionEnumerator Clone(CompositeTypeBinding resultTypeBindClone, VariableArray varArrClone)
    {
        INumericalExpression fetchNumberExprClone = null;
        if (fetchNumberExpr != null)
            fetchNumberExprClone = fetchNumberExpr.CloneToNumerical(varArrClone);

        return new ReferenceLookup(resultTypeBindClone, extentNumber, expression.CloneToObject(varArrClone),
            condition.Clone(varArrClone), fetchNumberExprClone, varArrClone, query);
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ReferenceLookup(");
        stringBuilder.AppendLine(tabs + 1, extentNumber.ToString());
        expression.BuildString(stringBuilder, tabs + 1);
        condition.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expression.GenerateCompilableCode(stringGen);
    }

    /// <summary>
    /// Gets the unique name for this enumerator.
    /// </summary>
    public String GetUniqueName(UInt64 seqNumber)
    {
        if (uniqueGenName == null)
            uniqueGenName = "ReferenceLookup" + extentNumber;

        return uniqueGenName;
    }
}
}
