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
    Row contextObject;

    IObjectExpression expression;
    ILogicalExpression condition;

    Boolean stayAtOffsetkey = false;
    public Boolean StayAtOffsetkey { get { return stayAtOffsetkey; } set { stayAtOffsetkey = value; } }
    Boolean useOffsetkey = true;
    public Boolean UseOffsetkey { get { return useOffsetkey; } set { useOffsetkey = value; } }

    internal ReferenceLookup(byte nodeId, RowTypeBinding rowTypeBind,
        Int32 extNum,
        IObjectExpression expr,
        ILogicalExpression cond,
        INumericalExpression fetchNumExpr,
        VariableArray varArr, String query)
        : base(nodeId, EnumeratorNodeType.ReferenceLookup, rowTypeBind, varArr)
    {
        if (rowTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");
        if (varArr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect varArr.");
        if (expr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr.");
        if (cond == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect cond.");

        extentNumber = extNum;

        currentObject = null;
        contextObject = null;
        expression = expr;
        condition = cond;

        fetchNumberExpr = fetchNumExpr;

        this.query = query;
    }

    /// <summary>
    /// The type binding of the resulting objects of the query.
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            if (projectionTypeCode == null)
                return rowTypeBinding;

            // Singleton object.
            if (projectionTypeCode == DbTypeCode.Object)
                return rowTypeBinding.GetPropertyBinding(0).TypeBinding;

            // Singleton non-object.
            return null;
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
                switch (projectionTypeCode)
                {
                    case null:
                        return currentObject;

                    case DbTypeCode.Binary:
                        return currentObject.GetBinary(0);

                    case DbTypeCode.Boolean:
                        return currentObject.GetBoolean(0);

                    case DbTypeCode.Byte:
                        return currentObject.GetByte(0);

                    case DbTypeCode.DateTime:
                        return currentObject.GetDateTime(0);

                    case DbTypeCode.Decimal:
                        return currentObject.GetDecimal(0);

                    case DbTypeCode.Double:
                        return currentObject.GetDouble(0);

                    case DbTypeCode.Int16:
                        return currentObject.GetInt16(0);

                    case DbTypeCode.Int32:
                        return currentObject.GetInt32(0);

                    case DbTypeCode.Int64:
                        return currentObject.GetInt64(0);

                    case DbTypeCode.Object:
                        return currentObject.GetObject(0);

                    case DbTypeCode.SByte:
                        return currentObject.GetSByte(0);

                    case DbTypeCode.Single:
                        return currentObject.GetSingle(0);

                    case DbTypeCode.String:
                        return currentObject.GetString(0);

                    case DbTypeCode.UInt16:
                        return currentObject.GetUInt16(0);

                    case DbTypeCode.UInt32:
                        return currentObject.GetUInt32(0);

                    case DbTypeCode.UInt64:
                        return currentObject.GetUInt64(0);

                    default:
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect projectionTypeCode.");
                }
            }

            throw new InvalidOperationException("Enumerator has not started or has already finished.");
        }
    }

    public Row CurrentRow
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
        if (useOffsetkey && !stayAtOffsetkey) {
            currentObject = null;
            return false;
        }

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
                currentObject = new Row(rowTypeBinding);
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
            NullObject nullObj = new NullObject(rowTypeBinding.GetTypeBinding(extentNumber));
            currentObject = new Row(rowTypeBinding);
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
    public override void Reset(Row obj)
    {
        contextObject = obj;
        currentObject = null;
        counter = 0;

        if (obj == null) {
            stayAtOffsetkey = false;
            useOffsetkey = true;
        }
    }

    public Boolean IsAtRecreatedKey {
        get { return currentObject != null; }
    }

    /// <summary>
    /// Controls that the object obj is in the current extent (the extent of this reference-lookup).
    /// The object's type must be equal to or a subtype of the extent type.
    /// </summary>
    /// <param name="obj">The object to control.</param>
    /// <returns>True, if the object is in the current extent, otherwise false.</returns>
    internal Boolean InCurrentExtent(IObjectView obj)
    {
        if (rowTypeBinding.GetTypeBinding(extentNumber) is TypeBinding && obj.TypeBinding is TypeBinding)
        {
            TypeBinding extentTypeBind = rowTypeBinding.GetTypeBinding(extentNumber) as TypeBinding;
            TypeBinding tmpTypeBind = obj.TypeBinding as TypeBinding;
            return tmpTypeBind.SubTypeOf(extentTypeBind);
        }
        else
        {
            return false;
        }
    }

    public override IExecutionEnumerator Clone(RowTypeBinding rowTypeBindClone, VariableArray varArrClone)
    {
        INumericalExpression fetchNumberExprClone = null;
        if (fetchNumberExpr != null)
            fetchNumberExprClone = fetchNumberExpr.CloneToNumerical(varArrClone);

        return new ReferenceLookup(nodeId, rowTypeBindClone, extentNumber, expression.CloneToObject(varArrClone),
            condition.Clone(varArrClone), fetchNumberExprClone, varArrClone, query);
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ReferenceLookup(");
        stringBuilder.AppendLine(tabs + 1, extentNumber.ToString());
        expression.BuildString(stringBuilder, tabs + 1);
        condition.BuildString(stringBuilder, tabs + 1);
        base.BuildFetchString(stringBuilder, tabs + 1);
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
