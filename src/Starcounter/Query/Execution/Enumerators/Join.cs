// ***********************************************************************
// <copyright file="Join.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
internal class Join : ExecutionEnumerator, IExecutionEnumerator
{
    JoinType joinType;
    IExecutionEnumerator leftEnumerator;
    IExecutionEnumerator rightEnumerator;
    CompositeObject contextObject;

    internal Join(CompositeTypeBinding compTypeBind,
        JoinType type,
        IExecutionEnumerator leftEnum,
        IExecutionEnumerator rightEnum,
        INumericalExpression fetchNumExpr,
        VariableArray varArr, String query)
        : base(varArr)
    {
        if (compTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compTypeBind.");
        if (leftEnum == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect leftEnum.");
        if (rightEnum == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rightEnum.");
        if (varArr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect variables clone.");

        // if (leftEnum.CompositeTypeBinding != compTypeBind)
        //    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incompatible input enumerator leftEnum.");
        // if (rightEnum.CompositeTypeBinding != compTypeBind)
        //    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incompatible input enumerator rightEnum.");

        compTypeBinding = compTypeBind;
        joinType = type;
        leftEnumerator = leftEnum;
        rightEnumerator = rightEnum;
        contextObject = null;
        currentObject = null;

        fetchNumberExpr = fetchNumExpr;
        fetchNumber = Int64.MaxValue;

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

    public Int32 Depth
    {
        get
        {
            if (rightEnumerator.Counter > 1)
            {
                return leftEnumerator.Depth + rightEnumerator.Depth + 1;
            }
            return leftEnumerator.Depth;
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

    /// <summary>
    /// Resets the enumerator with a context object.
    /// </summary>
    /// <param name="obj">Context object from another enumerator.</param>
    public override void Reset(CompositeObject obj)
    {
        currentObject = null;
        contextObject = obj;
        counter = 0;

        leftEnumerator.Reset(contextObject);
        rightEnumerator.Reset();
    }

    // TODO: Not create a new result object when not necessary.
    private CompositeObject MergeObjects(CompositeObject obj1, CompositeObject obj2)
    {
        CompositeObject obj = new CompositeObject(compTypeBinding);
        for (Int32 i = 0; i < compTypeBinding.TypeBindingCount; i++)
        {
            IObjectView element1 = null;
            IObjectView element2 = null;
            if (obj1 != null)
            {
                element1 = obj1.AccessObject(i);
            }
            if (obj2 != null)
            {
                element2 = obj2.AccessObject(i);
            }
            if (element1 != null && element2 == null)
            {
                obj.AttachObject(i, element1);
            }
            else if (element1 == null && element2 != null)
            {
                obj.AttachObject(i, element2);
            }
            else if (element1 == null && element2 == null)
            {
                obj.AttachObject(i, null);
            }
            else
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting objects.");
            }
        }
        return obj;
    }

    public Boolean MoveNext()
    {
        // If first call to MoveNext then move to first item in enumerator1.
        if (counter == 0)
        {
            if (fetchNumberExpr != null)
            {
                if (fetchNumberExpr.EvaluateToInteger(null) != null)
                    fetchNumber = fetchNumberExpr.EvaluateToInteger(null).Value;
                else
                    fetchNumber = 0;
                if (counter >= fetchNumber)
                {
                    currentObject = null;
                    return false;
                }
            }

            if (leftEnumerator.MoveNext())
            {
                CompositeObject rightContext = MergeObjects(contextObject, leftEnumerator.CurrentCompositeObject);
                rightEnumerator.Reset(rightContext);
            }
            else
            {
                currentObject = null;
                return false;
            }
        }
        else if (counter >= fetchNumber)
        {
            currentObject = null;
            return false;
        }

        // Loop until a new combination of one object from enumerator1 and one object from enumerator2 is found.
        if (joinType == JoinType.LeftOuter)
        {
            while (!rightEnumerator.MoveNextSpecial(false))
            {
                if (leftEnumerator.MoveNext())
                {
                    CompositeObject rightContext = MergeObjects(contextObject, leftEnumerator.CurrentCompositeObject);
                    rightEnumerator.Reset(rightContext);
                }
                else
                {
                    currentObject = null;
                    return false;
                }
            }
        }
        else
        {
            while (!rightEnumerator.MoveNext())
            {
                if (leftEnumerator.MoveNext())
                {
                    CompositeObject rightContext = MergeObjects(contextObject, leftEnumerator.CurrentCompositeObject);
                    rightEnumerator.Reset(rightContext);
                }
                else
                {
                    currentObject = null;
                    return false;
                }
            }
        }
        currentObject = MergeObjects(leftEnumerator.CurrentCompositeObject, rightEnumerator.CurrentCompositeObject);
        counter++;
        return true;
    }

    public Boolean MoveNextSpecial(Boolean force)
    {
        if (!force && MoveNext())
        {
            return true;
        }
        else if (counter == 0 || force)
        {
            // Force the creation of NullObjects on the input enumerators.
            leftEnumerator.MoveNextSpecial(true);
            CompositeObject rightContext = MergeObjects(contextObject, leftEnumerator.CurrentCompositeObject);
            rightEnumerator.Reset(rightContext);
            rightEnumerator.MoveNextSpecial(true);

            // Create new currentObject.
            currentObject = MergeObjects(leftEnumerator.CurrentCompositeObject, rightEnumerator.CurrentCompositeObject);
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
        Int32 offset = leftEnumerator.SaveEnumerator(keysData, globalOffset, saveDynamicDataOnly);
        offset = rightEnumerator.SaveEnumerator(keysData, offset, saveDynamicDataOnly);
        return offset;
    }

    /// <summary>
    /// Depending on query flags, populates the flags value.
    /// </summary>
    public unsafe override void PopulateQueryFlags(UInt32* flags)
    {
        leftEnumerator.PopulateQueryFlags(flags);
        rightEnumerator.PopulateQueryFlags(flags);
    }

    private void DebugPresent(String str, CompositeObject compObj)
    {
        String objIds = str + "Join ";
        for (Int32 i = 0; i < compTypeBinding.TypeBindingCount; i++)
        {
            IObjectView obj = compObj.AccessObject(i);
            if (obj != null)
            {
                objIds += i + ":" + obj.TypeBinding.Name + ":" + obj.ToString() + " ";
            }
            else
            {
                objIds += i + ":" + Starcounter.Db.NullString + " ";
            }
        }
        LogSources.Sql.Debug(objIds);
    }

    public override IExecutionEnumerator Clone(CompositeTypeBinding resultTypeBindClone, VariableArray varArrClone)
    {
        INumericalExpression fetchNumberExprClone = null;
        if (fetchNumberExpr != null)
            fetchNumberExprClone = fetchNumberExpr.CloneToNumerical(varArrClone);

        return new Join(resultTypeBindClone, joinType, leftEnumerator.Clone(resultTypeBindClone, varArrClone),
            rightEnumerator.Clone(resultTypeBindClone, varArrClone), fetchNumberExprClone, varArrClone, query);
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "Join(");
        stringBuilder.AppendLine(tabs + 1, joinType.ToString());
        leftEnumerator.BuildString(stringBuilder, tabs + 1);
        rightEnumerator.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        String enumName = GetUniqueName(stringGen.SeqNumber());
        String leftEnumName = leftEnumerator.GetUniqueName(stringGen.SeqNumber());
        String rightEnumName = rightEnumerator.GetUniqueName(stringGen.SeqNumber());

        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA Scan *g_" + enumName + " = 0;" + CodeGenStringGenerator.ENDL);
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.INIT_DATA, "g_" + enumName + " = new Scan(0" + ", " + enumName + "_CalculateRange);");

        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.DECLARATIONS, "INTERNAL_FUNCTION INT32 " + enumName + "_MoveNext();");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "INTERNAL_FUNCTION INT32 " + enumName + "_MoveNext()");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "return g_" + enumName + "->MoveNext();");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}" + CodeGenStringGenerator.ENDL);

        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.DECLARATIONS, "INTERNAL_FUNCTION INT32 " + enumName + "_Reset();");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "INTERNAL_FUNCTION INT32 " + enumName + "_Reset()");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "return g_" + enumName + "->Reset();");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}" + CodeGenStringGenerator.ENDL);

        if (joinType == JoinType.LeftOuter)
        {

        }
        else
        {

        }
    }

    /// <summary>
    /// Gets the unique name for this enumerator.
    /// </summary>
    public String GetUniqueName(UInt64 seqNumber)
    {
        if (uniqueGenName == null)
            uniqueGenName = "Join" + seqNumber;

        return uniqueGenName;
    }
}
}
